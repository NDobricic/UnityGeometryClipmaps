using UnityEngine;

public partial class GPUClipmapTerrain
{
    class TerrainCenter : TerrainChunk
    {
        private GameObject[] _squareChunks = new GameObject[16];
        private GameObject _centerCross;

        public TerrainCenter(TerrainData terrainData, Shader heightmapShader, CustomRenderTexture lowResHeightmap) 
            : base(0, terrainData, heightmapShader, lowResHeightmap)
        {
            GenerateSquareChunks();
            GenerateCenterCross();
        }

        public void UpdateChunkPositions(Vector3 playerPosition)
        {
            var chunkResolution = _terrainData.ChunkResolution;

            var offset = new Vector3(
                Mathf.Floor(playerPosition.x / 2) * 2 + 1,
                0,
                Mathf.Floor(playerPosition.z / 2) * 2 + 1);

            UpdateHeightmap(playerPosition, offset);

            // Update square chunks
            int squareChunkIndex = 0;
            for (int x = 0; x < 4; x++)
            {
                for (int z = 0; z < 4; z++)
                {
                    int offsetX = (x - 2) * (chunkResolution - 1) + ((x > 1) ? 1 : -1);
                    int offsetZ = (z - 2) * (chunkResolution - 1) + ((z > 1) ? 1 : -1);

                    _squareChunks[squareChunkIndex++].transform.localPosition =
                    new Vector3(offsetX, 0, offsetZ) + offset;
                }
            }

            // Update center cross
            _centerCross.transform.localPosition = offset;
        }

        void GenerateCenterCross()
        {
            _centerCross = InstantiatePiece(_terrainData.CenterCrossData.Mesh, _terrainData.CenterCrossData.Material,
                               new Vector3(0, 0, 0), new Vector3(1, 1, 1), "CenterCross");
        }

        void GenerateSquareChunks()
        {
            var squareChunkData = _terrainData.SquareChunkData;

            int chunkIndex = 0;
            for (int x = 0; x < 4; x++)
            {
                for (int z = 0; z < 4; z++)
                {
                    int offsetX = (x - 2) * (_terrainData.ChunkResolution - 1) + ((x > 1) ? 1 : -1);
                    int offsetZ = (z - 2) * (_terrainData.ChunkResolution - 1) + ((z > 1) ? 1 : -1);

                    _squareChunks[chunkIndex++] =
                        InstantiatePiece(squareChunkData.Mesh, squareChunkData.Material,
                        new Vector3(offsetX, 0, offsetZ), new Vector3(1, 1, 1), $"Square{chunkIndex}_Center");
                }
            }
        }
    }
}
