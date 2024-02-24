using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class GPUClipmapTerrain : MonoBehaviour
{
    class TerrainData
    {
        public int ChunkResolution;
        public Transform Parent;
        public ChunkPieceInfo SquareChunkData;
        public ChunkPieceInfo BorderVerticalData;
        public ChunkPieceInfo BorderHorizontalData;
        public ChunkPieceInfo InteriorVerticalData;
        public ChunkPieceInfo InteriorHorizontalData;
        public ChunkPieceInfo CenterCrossData;
    }
    class ChunkPieceInfo
    {
        public Mesh Mesh;
        public Material Material;
    }

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

    public Transform player;
    public int chunkResolution = 16;
    public int numberOfLevels = 3;

    private TerrainRing[] _terrainLevels;
    private TerrainCenter _terrainCenter;

    void Start()
    {
        _terrainLevels = new TerrainRing[numberOfLevels];
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        Mesh squareChunkMesh = CreatePlaneMesh(chunkResolution, chunkResolution);
        Mesh borderVerticalMesh = CreatePlaneMesh(chunkResolution, 3);
        Mesh borderHorizontalMesh = CreatePlaneMesh(3, chunkResolution);
        Mesh interiorVerticalMesh = CreatePlaneMesh(2 * chunkResolution + 1, 2);
        Mesh interiorHorizontalMesh = CreatePlaneMesh(2, 2 * chunkResolution + 1);
        Mesh centerCrossMesh = CreateCrossMesh(chunkResolution * 4 - 1);

        Material material = new Material(Shader.Find("Custom/ChunkShader"));
        material.SetFloat("_TileSize", 4f * chunkResolution - 1);

        TerrainData terrainData = new TerrainData
        {
            ChunkResolution = chunkResolution,
            Parent = transform,
            SquareChunkData = new ChunkPieceInfo { Mesh = squareChunkMesh, Material = material },
            BorderVerticalData = new ChunkPieceInfo { Mesh = borderVerticalMesh, Material = material },
            BorderHorizontalData = new ChunkPieceInfo { Mesh = borderHorizontalMesh, Material = material },
            InteriorVerticalData = new ChunkPieceInfo { Mesh = interiorVerticalMesh, Material = material },
            InteriorHorizontalData = new ChunkPieceInfo { Mesh = interiorHorizontalMesh, Material = material },
            CenterCrossData = new ChunkPieceInfo { Mesh = centerCrossMesh, Material = material }
        };

        Shader heightmapShader = Shader.Find("Custom/HeightmapShader");

        _terrainCenter = new TerrainCenter(terrainData, heightmapShader, null);
        for (int i = numberOfLevels - 1; i >= 0; i--)
        {
            if (i == numberOfLevels - 1)
            {
                _terrainLevels[i] = new TerrainRing(i + 1, terrainData, heightmapShader, null);
            }
            else
            {
                _terrainLevels[i] = new TerrainRing(i + 1, terrainData, heightmapShader, _terrainLevels[i + 1].Heightmap);
            }
        }
    }

    Mesh CreatePlaneMesh(int n, int m)
    {
        Mesh mesh = new Mesh();

        // Calculate number of vertices
        Vector3[] vertices = new Vector3[n * m];
        int[] triangles = new int[(n - 1) * (m - 1) * 6];
        Vector2[] uvs = new Vector2[n * m];

        float width = n - 1;
        float height = m - 1;

        // Generate vertices and UVs
        for (int i = 0, z = 0; z < m; z++)
        {
            for (int x = 0; x < n; x++, i++)
            {
                vertices[i] = new Vector3(x, 0, z);
                uvs[i] = new Vector2((float)x / width, (float)z / height);
            }
        }

        // Generate triangles
        int ti = 0;
        for (int z = 0; z < m - 1; z++)
        {
            for (int x = 0; x < n - 1; x++)
            {
                int start = x + z * n;
                triangles[ti] = start;
                triangles[ti + 1] = start + n;
                triangles[ti + 2] = start + n + 1;
                triangles[ti + 3] = start;
                triangles[ti + 4] = start + n + 1;
                triangles[ti + 5] = start + 1;
                ti += 6;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();

        return mesh;
    }

    Mesh CreateCrossMesh(int size)
    {
        Mesh mesh = new Mesh();

        var vertices = new Vector3[size * 3 * 2];
        var triangles = new int[(size - 1) * 4 * 2 * 3];

        // Generate vertical arm
        for (int i = 0; i < size; i++)
        {
            vertices[i * 3] = new Vector3(-1, 0, size / 2 - i);
            vertices[i * 3 + 1] = new Vector3(0, 0, size / 2 - i);
            vertices[i * 3 + 2] = new Vector3(1, 0, size / 2 - i);
        }

        for (int i = 0; i < size - 1; i++)
        {
            int ti = i * 12;
            int vi = i * 3;

            triangles[ti] = vi;
            triangles[ti + 1] = vi + 1;
            triangles[ti + 2] = vi + 3;
            triangles[ti + 3] = vi + 1;
            triangles[ti + 4] = vi + 4;
            triangles[ti + 5] = vi + 3;

            triangles[ti + 6] = vi + 1;
            triangles[ti + 7] = vi + 2;
            triangles[ti + 8] = vi + 4;
            triangles[ti + 9] = vi + 2;
            triangles[ti + 10] = vi + 5;
            triangles[ti + 11] = vi + 4;
        }

        // Generate horizontal arm
        for (int i = 0; i < size; i++)
        {
            vertices[(size + i) * 3] = new Vector3(size / 2 - i, 0, -1);
            vertices[(size + i) * 3 + 1] = new Vector3(size / 2 - i, 0, 0);
            vertices[(size + i) * 3 + 2] = new Vector3(size / 2 - i, 0, 1);
        }

        for (int i = 0; i < size - 1; i++)
        {
            int ti = (size - 1) * 12 + i * 12;
            int vi = (size + i) * 3;

            triangles[ti] = vi;
            triangles[ti + 1] = vi + 3;
            triangles[ti + 2] = vi + 1;
            triangles[ti + 3] = vi + 1;
            triangles[ti + 4] = vi + 3;
            triangles[ti + 5] = vi + 4;

            triangles[ti + 6] = vi + 1;
            triangles[ti + 7] = vi + 4;
            triangles[ti + 8] = vi + 2;
            triangles[ti + 9] = vi + 2;
            triangles[ti + 10] = vi + 4;
            triangles[ti + 11] = vi + 5;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }



    void Update()
    {
        _terrainCenter.UpdateChunkPositions(player.position);
        foreach (var terrainLevel in _terrainLevels)
        {
            terrainLevel.UpdateChunkPositions(player.position);
        }
    }
}
