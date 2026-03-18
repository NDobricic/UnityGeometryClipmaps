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
                material = new Material(heightmapShader)
            };

            _heightmap.material.SetFloat("_NoiseFrequency", 1f / (1 << level));
            _heightmap.material.SetFloat("_Size", (4f * terrainData.ChunkResolution - 1) * (1 << level));
            _heightmap.material.SetFloat("_MaxHeight", terrainData.MaxHeight);
            _heightmap.material.SetFloat("_NoiseScale", terrainData.NoiseScale);
            _heightmap.material.SetVector("_Offset", new Vector4(terrainData.NoiseOffset.x, 0, terrainData.NoiseOffset.y, 0));
            _heightmap.material.SetInt("_Octaves", terrainData.Octaves);
            _heightmap.material.SetFloat("_Lacunarity", terrainData.Lacunarity);
            _heightmap.material.SetFloat("_Persistence", terrainData.Persistence);
            _heightmap.Update();
        }

        public void UpdateNoiseParameters(float maxHeight, float noiseScale, Vector2 noiseOffset, int octaves, float lacunarity, float persistence)
        {
            _heightmap.material.SetFloat("_MaxHeight", maxHeight);
            _heightmap.material.SetFloat("_NoiseScale", noiseScale);
            _heightmap.material.SetVector("_Offset", new Vector4(noiseOffset.x, 0, noiseOffset.y, 0));
            _heightmap.material.SetInt("_Octaves", octaves);
            _heightmap.material.SetFloat("_Lacunarity", lacunarity);
            _heightmap.material.SetFloat("_Persistence", persistence);
        }

        protected void SetLevelCenter(Vector3 center)
        {
            foreach (var updater in _chunkUpdaters)
                updater.levelCenter = center;
        }

        protected void UpdateHeightmap(Vector3 playerPosition, Vector3 offset)
        {
            var size = new Vector3(1 << _level, 1, 1 << _level);
            var tileSize = 4 * _terrainData.ChunkResolution - 1;

            var playerPosNormalized =
                new Vector3((playerPosition.x + offset.x) / size.x, (playerPosition.z + offset.z) / size.z, 0) / tileSize / 2;
            _heightmap.material.SetVector("_Origin", playerPosNormalized);
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
