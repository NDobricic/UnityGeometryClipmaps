using System.Collections.Generic;
using UnityEngine;

public partial class GPUClipmapTerrain
{
    class TerrainChunk
    {
        public CustomRenderTexture Heightmap => _heightmap;

        protected TerrainData _terrainData;
        protected int _level;

        protected CustomRenderTexture _heightmap;
        protected CustomRenderTexture _lowResHeightmap;

        protected Vector3 _previousPlayerPos;
        protected List<ChunkUpdater> _chunkUpdaters = new List<ChunkUpdater>();

        // Toroidal update tracking
        private int _prevWrapCountX, _prevWrapCountZ;
        private int _prevEpochX, _prevEpochZ;
        private bool _forceFullUpdate = true;

        // Cached noise parameters for change detection
        private float _cachedMaxHeight, _cachedNoiseScale, _cachedLacunarity, _cachedPersistence;
        private Vector2 _cachedNoiseOffset;
        private int _cachedOctaves;

        private static readonly Vector4 ClipDisabled = new Vector4(-1, -1, 0, 0);

        public TerrainChunk(int level, TerrainData terrainData, Shader heightmapShader, CustomRenderTexture lowResHeightmap)
        {
            _previousPlayerPos = Vector3.zero;

            _level = level;
            _terrainData = terrainData;
            _lowResHeightmap = lowResHeightmap;

            _heightmap = new CustomRenderTexture(terrainData.ChunkResolution * 4 - 1,
                terrainData.ChunkResolution * 4 - 1, RenderTextureFormat.ARGBFloat)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                material = new Material(heightmapShader),
                updateMode = CustomRenderTextureUpdateMode.OnDemand
            };

            _heightmap.material.SetFloat("_NoiseFrequency", 1f / (1 << level));
            _heightmap.material.SetFloat("_Size", (4f * terrainData.ChunkResolution - 1) * (1 << level));
            _heightmap.material.SetFloat("_MaxHeight", terrainData.MaxHeight);
            _heightmap.material.SetFloat("_NoiseScale", terrainData.NoiseScale);
            _heightmap.material.SetVector("_Offset", new Vector4(terrainData.NoiseOffset.x, 0, terrainData.NoiseOffset.y, 0));
            _heightmap.material.SetInt("_Octaves", terrainData.Octaves);
            _heightmap.material.SetFloat("_Lacunarity", terrainData.Lacunarity);
            _heightmap.material.SetFloat("_Persistence", terrainData.Persistence);
            _heightmap.material.SetVector("_ClipX", ClipDisabled);
            _heightmap.material.SetVector("_ClipZ", ClipDisabled);

            _cachedMaxHeight = terrainData.MaxHeight;
            _cachedNoiseScale = terrainData.NoiseScale;
            _cachedNoiseOffset = terrainData.NoiseOffset;
            _cachedOctaves = terrainData.Octaves;
            _cachedLacunarity = terrainData.Lacunarity;
            _cachedPersistence = terrainData.Persistence;
        }

        public void SetDebugPartialUpdates(bool enabled)
        {
            _heightmap.material.SetFloat("_DebugPartialUpdate", enabled ? 1f : 0f);
        }

        public void UpdateNoiseParameters(float maxHeight, float noiseScale, Vector2 noiseOffset, int octaves, float lacunarity, float persistence)
        {
            bool changed = maxHeight != _cachedMaxHeight || noiseScale != _cachedNoiseScale ||
                           noiseOffset != _cachedNoiseOffset || octaves != _cachedOctaves ||
                           lacunarity != _cachedLacunarity || persistence != _cachedPersistence;

            if (!changed) return;

            _heightmap.material.SetFloat("_MaxHeight", maxHeight);
            _heightmap.material.SetFloat("_NoiseScale", noiseScale);
            _heightmap.material.SetVector("_Offset", new Vector4(noiseOffset.x, 0, noiseOffset.y, 0));
            _heightmap.material.SetInt("_Octaves", octaves);
            _heightmap.material.SetFloat("_Lacunarity", lacunarity);
            _heightmap.material.SetFloat("_Persistence", persistence);

            _cachedMaxHeight = maxHeight;
            _cachedNoiseScale = noiseScale;
            _cachedNoiseOffset = noiseOffset;
            _cachedOctaves = octaves;
            _cachedLacunarity = lacunarity;
            _cachedPersistence = persistence;

            _forceFullUpdate = true;
        }

        protected void SetLevelCenter(Vector3 center)
        {
            foreach (var updater in _chunkUpdaters)
                updater.levelCenter = center;
        }

        /// <summary>
        /// Count how many texels satisfy the shader's wrap condition:
        ///   (i + 0.5) / tileSize  &lt;  frac(origin)
        /// </summary>
        private static int ComputeWrapCount(float originComponent, int tileSize)
        {
            float fracVal = originComponent - Mathf.Floor(originComponent);
            float boundary = fracVal * tileSize;
            if (boundary <= 0.5f) return 0;
            return Mathf.CeilToInt(boundary - 0.5f);
        }

        protected void UpdateHeightmap(Vector3 playerPosition, Vector3 offset)
        {
            var size = new Vector3(1 << _level, 1, 1 << _level);
            var tileSize = 4 * _terrainData.ChunkResolution - 1;

            var origin = new Vector3(
                (playerPosition.x + offset.x) / size.x,
                (playerPosition.z + offset.z) / size.z,
                0) / tileSize / 2;
            _heightmap.material.SetVector("_Origin", origin);

            int epochX = Mathf.FloorToInt(origin.x);
            int epochZ = Mathf.FloorToInt(origin.y);
            int wrapCountX = ComputeWrapCount(origin.x, tileSize);
            int wrapCountZ = ComputeWrapCount(origin.y, tileSize);

            // Full update on: first frame, parameter change, or coordinate epoch change
            if (_forceFullUpdate || epochX != _prevEpochX || epochZ != _prevEpochZ)
            {
                DoFullUpdate();
                _prevWrapCountX = wrapCountX;
                _prevWrapCountZ = wrapCountZ;
                _prevEpochX = epochX;
                _prevEpochZ = epochZ;
                _forceFullUpdate = false;
                return;
            }

            int dx = wrapCountX - _prevWrapCountX;
            int dz = wrapCountZ - _prevWrapCountZ;

            if (dx == 0 && dz == 0)
                return;

            // Large movement (teleport): full update
            if (Mathf.Abs(dx) > tileSize / 2 || Mathf.Abs(dz) > tileSize / 2)
            {
                DoFullUpdate();
            }
            else
            {
                DoPartialUpdate(dx, dz, tileSize);
            }

            _prevWrapCountX = wrapCountX;
            _prevWrapCountZ = wrapCountZ;
        }

        private void DoFullUpdate()
        {
            _heightmap.material.SetVector("_ClipX", ClipDisabled);
            _heightmap.material.SetVector("_ClipZ", ClipDisabled);
            _heightmap.Update();
        }

        private void DoPartialUpdate(int dx, int dz, int tileSize)
        {
            if (dx != 0)
            {
                int startIndex = Mathf.Min(_prevWrapCountX, _prevWrapCountX + dx);
                int count = Mathf.Abs(dx);
                startIndex = ((startIndex % tileSize) + tileSize) % tileSize;

                float minU = (float)startIndex / tileSize;
                float maxU = (float)(startIndex + count) / tileSize;
                if (maxU > 1f) maxU -= 1f; // min > max signals UV wrapping

                _heightmap.material.SetVector("_ClipX", new Vector4(minU, maxU, 0, 0));
            }
            else
            {
                _heightmap.material.SetVector("_ClipX", ClipDisabled);
            }

            if (dz != 0)
            {
                int startIndex = Mathf.Min(_prevWrapCountZ, _prevWrapCountZ + dz);
                int count = Mathf.Abs(dz);
                startIndex = ((startIndex % tileSize) + tileSize) % tileSize;

                float minV = (float)startIndex / tileSize;
                float maxV = (float)(startIndex + count) / tileSize;
                if (maxV > 1f) maxV -= 1f;

                _heightmap.material.SetVector("_ClipZ", new Vector4(minV, maxV, 0, 0));
            }
            else
            {
                _heightmap.material.SetVector("_ClipZ", ClipDisabled);
            }

            _heightmap.Update();
        }

        protected GameObject InstantiatePiece(Mesh mesh, Material material, Vector3 position, Vector3 size, Transform playerTransform, string name = "Chunk")
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(_terrainData.Parent);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;

            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            AdjustMeshBounds(meshFilter);

            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;

            var updater = obj.AddComponent<ChunkUpdater>();
            updater.player = playerTransform;
            _chunkUpdaters.Add(updater);

            var mbp = new MaterialPropertyBlock();
            mbp.SetTexture("_Heightmap", _heightmap);
            if (_lowResHeightmap != null)
            {
                mbp.SetTexture("_LowResHeightmap", _lowResHeightmap);
            }
            else
            {
                mbp.SetTexture("_LowResHeightmap", _heightmap);
            }
            meshRenderer.SetPropertyBlock(mbp);

            return obj;
        }

        private void AdjustMeshBounds(MeshFilter meshFilter)
        {
            Bounds bounds = meshFilter.mesh.bounds;
            bounds.extents = new Vector3(bounds.extents.x, _terrainData.MaxHeight, bounds.extents.z);
            bounds.center = new Vector3(bounds.center.x, 0f, bounds.center.z);
            meshFilter.mesh.bounds = bounds;
        }
    }
}
