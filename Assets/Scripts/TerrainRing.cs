using UnityEngine;

public partial class GPUClipmapTerrain
{
    class TerrainRing : TerrainChunk
    {
        private GameObject[] _squareChunks = new GameObject[12];
        private GameObject[] _verticalBorders = new GameObject[2];
        private GameObject[] _horizontalBorders = new GameObject[2];
        private GameObject _interiorVertical;
        private GameObject _interiorHorizontal;

        public TerrainRing(int level, TerrainData terrainData, Shader heightmapShader, CustomRenderTexture lowResHeightmap)
            : base(level, terrainData, heightmapShader, lowResHeightmap)
        {
            GenerateSquareChunks();
            GenerateBorderFillers();
            GenerateInteriorFillers();
        }

        public void UpdateChunkPositions(Vector3 playerPosition)
        {
            var size = new Vector3(1 << _level, 1, 1 << _level);
            var chunkResolution = _terrainData.ChunkResolution;

            var offset = new Vector3(
                Mathf.Floor(playerPosition.x / (2 << _level)) * (2 << _level) + size.x,
                0,
                Mathf.Floor(playerPosition.z / (2 << _level)) * (2 << _level) + size.z);

            UpdateHeightmap(playerPosition, offset);

            // Update square chunks
            int squareChunkIndex = 0;
            for (int x = 0; x < 4; x++)
            {
                for (int z = 0; z < 4; z++)
                {
                    if (x == 0 || x == 3 || z == 0 || z == 3)
                    {
                        int offsetX = (x - 2) * (chunkResolution - 1) + ((x > 1) ? 1 : -1);
                        int offsetZ = (z - 2) * (chunkResolution - 1) + ((z > 1) ? 1 : -1);

                        _squareChunks[squareChunkIndex++].transform.localPosition =
                            new Vector3(offsetX * size.x, 0, offsetZ * size.z) + offset;
                    }
                }
            }

            // Update border fillers
            _verticalBorders[0].transform.localPosition = new Vector3(size.x * ((chunkResolution - 1) + 1), 0, -size.z) + offset;
            _verticalBorders[1].transform.localPosition = new Vector3(-size.x * (2 * (chunkResolution - 1) + 1), 0, -size.z) + offset;

            _horizontalBorders[0].transform.localPosition = new Vector3(-size.x, 0, size.z * ((chunkResolution - 1) + 1)) + offset;
            _horizontalBorders[1].transform.localPosition = new Vector3(-size.x, 0, -size.z * (2 * (chunkResolution - 1) + 1)) + offset;

            // Update interior fillers
            var innerOffset = new Vector3(
                Mathf.Floor(playerPosition.x / (1 << _level)) * (1 << _level),
                0,
                Mathf.Floor(playerPosition.z / (1 << _level)) * (1 << _level));
            var offsetDiff = offset - innerOffset;

            _interiorVertical.transform.localPosition = 
                new Vector3(-size.x * ((chunkResolution - 1) + 1),
                0, (offsetDiff.z != 0) ? size.z * (chunkResolution - 1) : -size.z * ((chunkResolution - 1) + 1))
            + offset;

            _interiorHorizontal.transform.localPosition =
                new Vector3((offsetDiff.x != 0) ? size.x * (chunkResolution - 1) : -size.x * ((chunkResolution - 1) + 1),
                0, -size.z * ((chunkResolution - 1) + 1))
            + offset;
        }

        void GenerateInteriorFillers()
        {
            var size = new Vector3(1 << _level, 1, 1 << _level);

            // Generate vertical filler
            _interiorVertical = InstantiatePiece(_terrainData.InteriorVerticalData.Mesh,
                _terrainData.InteriorVerticalData.Material, new Vector3(0, 0, 0), size, $"InteriorVertical_{_level}");

            // Generate horizontal filler
            _interiorHorizontal = InstantiatePiece(_terrainData.InteriorHorizontalData.Mesh,
                _terrainData.InteriorHorizontalData.Material, new Vector3(0, 0, 0), size, $"InteriorHorizontal_{_level}");
        }

        void GenerateSquareChunks()
        {
            var size = new Vector3(1 << _level, 1, 1 << _level);
            var squareChunkData = _terrainData.SquareChunkData;

            int chunkIndex = 0;
            for (int x = 0; x < 4; x++)
            {
                for (int z = 0; z < 4; z++)
                {
                    if (x == 0 || x == 3 || z == 0 || z == 3)
                    {
                        int offsetX = (x - 2) * (_terrainData.ChunkResolution - 1) + ((x > 1) ? 1 : -1);
                        int offsetZ = (z - 2) * (_terrainData.ChunkResolution - 1) + ((z > 1) ? 1 : -1);

                        _squareChunks[chunkIndex++] =
                            InstantiatePiece(squareChunkData.Mesh, squareChunkData.Material,
                            new Vector3(offsetX * size.x, 0, offsetZ * size.z), size, $"Square{chunkIndex}_{_level}");
                    }
                }
            }
        }

        void GenerateBorderFillers()
        {
            var size = new Vector3(1 << _level, 1, 1 << _level);
            var verticalData = _terrainData.BorderVerticalData;
            var horizontalData = _terrainData.BorderHorizontalData;

            // Generate vertical fillers
            _verticalBorders[0] = InstantiatePiece(verticalData.Mesh, verticalData.Material,
                new Vector3(size.x * ((_terrainData.ChunkResolution - 1) + 1), 0, -size.z), size, $"VerticalBorder0_{_level}");
            _verticalBorders[1] = InstantiatePiece(verticalData.Mesh, verticalData.Material,
                new Vector3(-size.x * (2 * (_terrainData.ChunkResolution - 1) + 1), 0, -size.z), size, $"VerticalBorder1_{_level}");

            // Generate horizontal fillers
            _horizontalBorders[0] = InstantiatePiece(horizontalData.Mesh, horizontalData.Material,
                new Vector3(-size.x, 0, size.z * ((_terrainData.ChunkResolution - 1) + 1)), size, $"HorizontalBorder0_{_level}");
            _horizontalBorders[1] = InstantiatePiece(horizontalData.Mesh, horizontalData.Material,
                new Vector3(-size.x, 0, -size.z * (2 * (_terrainData.ChunkResolution - 1) + 1)), size, $"HorizontalBorder1_{_level}");
        }

    }
}
