using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPUClipmapTerrain : MonoBehaviour
{
    public Transform player;
    const int CHUNK_RESOLUTION = 16;

    // Start is called before the first frame update
    void Start()
    {
        Mesh squareChunkMesh = CreatePlaneMesh(CHUNK_RESOLUTION, CHUNK_RESOLUTION);
        Material squareChunkMaterial = new Material(Shader.Find("Custom/ChunkShader"));

        var size = new Vector3(3, 1, 3);

        for (int i = 0; i < 9; i++)
        {
            int x = i % 3;
            int z = i / 3;
            InstantiateObject(squareChunkMesh, squareChunkMaterial, 
                new Vector3(x * size.x, 0, z * size.z) * (CHUNK_RESOLUTION - 1), size);
        }
    }

    void InstantiateObject(Mesh mesh, Material material, Vector3 position, Vector3 size)
    {
        GameObject obj = new GameObject("Chunk");
        obj.transform.position = Vector3.zero;

        var meshFilter = obj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        var meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;

        MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        mpb.SetVector("_Offset", position);
        mpb.SetVector("_Size", size);
        meshRenderer.SetPropertyBlock(mpb);
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
