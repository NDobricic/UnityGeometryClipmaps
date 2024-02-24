using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public static class MeshGenerators
{
    public static Mesh CreatePlaneMesh(int n, int m)
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

    public static Mesh CreateCrossMesh(int size)
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
}
