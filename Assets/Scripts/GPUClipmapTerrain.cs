using System.Collections;
using System.Collections.Generic;
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
    }
    struct ChunkData
    {
        public Mesh Mesh;
        public Material Material;
    }

    class TerrainLevel
    {
        private TerrainData _terrainData;
        private int _level;

        private GameObject[] _squareChunks = new GameObject[12];
        private GameObject[] _verticalBorders = new GameObject[2];
        private GameObject[] _horizontalBorders = new GameObject[2];
        private GameObject _interiorVertical;
        private GameObject _interiorHorizontal;

        public TerrainLevel(int level, TerrainData terrainData)
        {
            _level = level;
            _terrainData = terrainData;

            GenerateSquareChunks();
            GenerateBorderFillers();
            //GenerateInteriorFillers(chunkResolution, level, interiorFillerData);
        }

        void UpdateChunkPositions(Vector3 playerPosition)
        {
            
        }

        void GenerateBorderFillers()
        {
            var size = new Vector3(1 << _level, 1, 1 << _level);
            var verticalData = _terrainData.BorderVerticalData;
            var horizontalData = _terrainData.BorderHorizontalData;

            // Generate vertical fillers
            _verticalBorders[0] = InstantiateObject(verticalData.Mesh, verticalData.Material,
                new Vector3(size.x * ((_terrainData.ChunkResolution - 1) + 1), 0, -size.z), size);
            _verticalBorders[1] = InstantiateObject(verticalData.Mesh, verticalData.Material,
                new Vector3(-size.x * (2 * (_terrainData.ChunkResolution - 1) + 1), 0, -size.z), size);

            // Generate horizontal fillers
            _horizontalBorders[0] = InstantiateObject(horizontalData.Mesh, horizontalData.Material,
                new Vector3(-size.x, 0, size.z * ((_terrainData.ChunkResolution - 1) + 1)), size);
            _horizontalBorders[1] = InstantiateObject(horizontalData.Mesh, horizontalData.Material,
                new Vector3(-size.x, 0, -size.z * (2 * (_terrainData.ChunkResolution - 1) + 1)), size);
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
                            new Vector3(offsetX * size.x, 0, offsetZ * size.z), size);
                    }
                }
            }
        }

        GameObject InstantiateObject(Mesh mesh, Material material, Vector3 position, Vector3 size)
        {
            GameObject obj = new GameObject("Chunk");
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
    public Transform player;
    public Texture2D heightmap;
    public int chunkResolution = 16;
    public int numberOfLevels = 3;

    private TerrainLevel[] _terrainLevels;

    void Start()
    {
        _terrainLevels = new TerrainLevel[numberOfLevels];
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        Mesh squareChunkMesh = CreatePlaneMesh(chunkResolution, chunkResolution);
        Mesh borderVerticalMesh = CreatePlaneMesh(chunkResolution, 3);
        Mesh borderHorizontalMesh = CreatePlaneMesh(3, chunkResolution);
        Mesh interiorVerticalMesh = CreatePlaneMesh(2 * (chunkResolution - 1) + 2, 2);
        Mesh interiorHorizontalMesh = CreatePlaneMesh(2, 2 * (chunkResolution - 1) + 2);

        Material material = new Material(Shader.Find("Custom/ChunkShader"));

        TerrainData terrainData = new TerrainData
        {
            ChunkResolution = chunkResolution,
            Parent = transform,
            SquareChunkData = new ChunkData { Mesh = squareChunkMesh, Material = material },
            BorderVerticalData = new ChunkData { Mesh = borderVerticalMesh, Material = material },
            BorderHorizontalData = new ChunkData { Mesh = borderHorizontalMesh, Material = material },
            InteriorVerticalData = new ChunkData { Mesh = interiorVerticalMesh, Material = material },
            InteriorHorizontalData = new ChunkData { Mesh = interiorHorizontalMesh, Material = material }
        };

        for (int i = 0; i < numberOfLevels; i++)
        {
            var terrainLevel = new TerrainLevel(i, terrainData);
            _terrainLevels[i] = terrainLevel;
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



    void Update()
    {
        
    }
}
