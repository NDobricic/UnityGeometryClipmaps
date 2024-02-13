using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class CPUClipmapTerrain : MonoBehaviour
{
    struct TerrainData
    {
        public int ChunkResolution;
        public Transform Parent;
        public ChunkData SquareChunkData;
        public ChunkData BorderVerticalData;
        public ChunkData BorderHorizontalData;
        public ChunkData InteriorVerticalData;
        public ChunkData InteriorHorizontalData;
        public ChunkData CenterCrossData;
    }
    struct ChunkData
    {
        public Mesh Mesh;
        public Material Material;
    }
    
    class TerrainCenter
    {
        private TerrainData _terrainData;
        private GameObject[] _squareChunks = new GameObject[16];
        private GameObject _centerCross;

        public TerrainCenter(TerrainData terrainData)
        {
            _terrainData = terrainData;

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
            _centerCross = InstantiateObject(_terrainData.CenterCrossData.Mesh, _terrainData.CenterCrossData.Material,
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
                        InstantiateObject(squareChunkData.Mesh, squareChunkData.Material,
                        new Vector3(offsetX, 0, offsetZ), new Vector3(1, 1, 1), $"Square{chunkIndex}_Center");
                }
            }
        }

        GameObject InstantiateObject(Mesh mesh, Material material, Vector3 position, Vector3 size, string name = "Chunk")
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

            return obj;
        }
    }

    class TerrainRing
    {
        public CustomRenderTexture Heightmap => _heightmap;

        private TerrainData _terrainData;
        private int _level;

        private GameObject[] _squareChunks = new GameObject[12];
        private GameObject[] _verticalBorders = new GameObject[2];
        private GameObject[] _horizontalBorders = new GameObject[2];
        private GameObject _interiorVertical;
        private GameObject _interiorHorizontal;
        private CustomRenderTexture _heightmap;
        private CustomRenderTexture _lowResHeightmap;

        private Vector3 _previousPlayerPos;

        public TerrainRing(int level, TerrainData terrainData, Shader heightmapShader, CustomRenderTexture lowResHeightmap)
        {
            _previousPlayerPos = Vector3.zero;

            _level = level;
            _terrainData = terrainData;
            _lowResHeightmap = lowResHeightmap;

            _heightmap = new CustomRenderTexture(terrainData.ChunkResolution * 4 - 1, 
                terrainData.ChunkResolution * 4 - 1, RenderTextureFormat.RFloat);
            _heightmap.wrapMode = TextureWrapMode.Repeat;
            _heightmap.filterMode = FilterMode.Point;
            _heightmap.useMipMap = false;
            _heightmap.material = new Material(heightmapShader);
            _heightmap.material.SetFloat("_NoiseFrequency", 1f / (1 << level));
            _heightmap.Update();

            GenerateSquareChunks();
            GenerateBorderFillers();
            GenerateInteriorFillers();
        }

        public void UpdateChunkPositions(Vector3 playerPosition)
        {
            var size = new Vector3(1 << _level, 1, 1 << _level);
            var chunkResolution = _terrainData.ChunkResolution;
            var tileSize = 4 * chunkResolution - 1;

            var offset = new Vector3(
                Mathf.Floor(playerPosition.x / (2 << _level)) * (2 << _level) + size.x,
                0,
                Mathf.Floor(playerPosition.z / (2 << _level)) * (2 << _level) + size.z);

            //_heightmap.material.SetVector("_Offset", 
            //    new Vector4(
            //        offset.x - (2 * (chunkResolution - 1) - 1), 
            //        0,
            //        offset.z - (2 * (chunkResolution - 1) - 1), 
            //        0));

            var playerPosNormalized =
                new Vector3((playerPosition.x + offset.x) / size.x, (playerPosition.z + offset.z) / size.z, 0) / tileSize / 2;
            _heightmap.material.SetVector("_Origin", playerPosNormalized);
            _heightmap.Update();

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

            _interiorVertical = InstantiateObject(_terrainData.InteriorVerticalData.Mesh,
                _terrainData.InteriorVerticalData.Material, new Vector3(0, 0, 0), size, $"InteriorVertical_{_level}");

            _interiorHorizontal = InstantiateObject(_terrainData.InteriorHorizontalData.Mesh,
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
                            InstantiateObject(squareChunkData.Mesh, squareChunkData.Material,
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
            _verticalBorders[0] = InstantiateObject(verticalData.Mesh, verticalData.Material,
                new Vector3(size.x * ((_terrainData.ChunkResolution - 1) + 1), 0, -size.z), size, $"VerticalBorder0_{_level}");
            _verticalBorders[1] = InstantiateObject(verticalData.Mesh, verticalData.Material,
                new Vector3(-size.x * (2 * (_terrainData.ChunkResolution - 1) + 1), 0, -size.z), size, $"VerticalBorder1_{_level}");

            // Generate horizontal fillers
            _horizontalBorders[0] = InstantiateObject(horizontalData.Mesh, horizontalData.Material,
                new Vector3(-size.x, 0, size.z * ((_terrainData.ChunkResolution - 1) + 1)), size, $"HorizontalBorder0_{_level}");
            _horizontalBorders[1] = InstantiateObject(horizontalData.Mesh, horizontalData.Material,
                new Vector3(-size.x, 0, -size.z * (2 * (_terrainData.ChunkResolution - 1) + 1)), size, $"HorizontalBorder1_{_level}");
        }

        GameObject InstantiateObject(Mesh mesh, Material material, Vector3 position, Vector3 size, string name = "Chunk")
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
            SquareChunkData = new ChunkData { Mesh = squareChunkMesh, Material = material },
            BorderVerticalData = new ChunkData { Mesh = borderVerticalMesh, Material = material },
            BorderHorizontalData = new ChunkData { Mesh = borderHorizontalMesh, Material = material },
            InteriorVerticalData = new ChunkData { Mesh = interiorVerticalMesh, Material = material },
            InteriorHorizontalData = new ChunkData { Mesh = interiorHorizontalMesh, Material = material },
            CenterCrossData = new ChunkData { Mesh = centerCrossMesh, Material = material }
        };

        Shader heightmapShader = Shader.Find("Custom/HeightmapShader");

        _terrainCenter = new TerrainCenter(terrainData);
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
