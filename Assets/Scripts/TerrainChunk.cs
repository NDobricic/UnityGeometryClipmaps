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

        public TerrainChunk(int level, TerrainData terrainData, Shader heightmapShader, CustomRenderTexture lowResHeightmap)
        {
            _previousPlayerPos = Vector3.zero;

            _level = level;
            _terrainData = terrainData;
            _lowResHeightmap = lowResHeightmap;

            _heightmap = new CustomRenderTexture(terrainData.ChunkResolution * 4 - 1,
                terrainData.ChunkResolution * 4 - 1, RenderTextureFormat.RFloat)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
                useMipMap = false,
                material = new Material(heightmapShader)
            };

            _heightmap.material.SetFloat("_NoiseFrequency", 1f / (1 << level));
            _heightmap.Update();
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

        protected GameObject InstantiatePiece(Mesh mesh, Material material, Vector3 position, Vector3 size, string name = "Chunk")
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(_terrainData.Parent);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;

            var meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            obj.AddComponent<ChunkUpdater>();
            var mbp = new MaterialPropertyBlock();
            mbp.SetTexture("_Heightmap", _heightmap);
            meshRenderer.SetPropertyBlock(mbp);

            return obj;
        }
    }
}
