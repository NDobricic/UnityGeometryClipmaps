using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public partial class GPUClipmapTerrain : MonoBehaviour
{
    public Transform player;
    public int chunkResolution = 16;
    public int numberOfLevels = 3;
    public float maxHeight = 50.0f;

    [Header("Noise")]
    public float noiseScale = 1f;
    public Vector2 noiseOffset = Vector2.zero;

    [Header("Debug")]
    public bool debugBlendVisualization = false;

    private TerrainRing[] _terrainLevels;
    private TerrainCenter _terrainCenter;
    private Material _chunkMaterial;

    void Start()
    {
        _terrainLevels = new TerrainRing[numberOfLevels];
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        Mesh squareChunkMesh = MeshGenerators.CreatePlaneMesh(chunkResolution, chunkResolution);
        Mesh borderVerticalMesh = MeshGenerators.CreatePlaneMesh(chunkResolution, 3);
        Mesh borderHorizontalMesh = MeshGenerators.CreatePlaneMesh(3, chunkResolution);
        Mesh interiorVerticalMesh = MeshGenerators.CreatePlaneMesh(2 * chunkResolution + 1, 2);
        Mesh interiorHorizontalMesh = MeshGenerators.CreatePlaneMesh(2, 2 * chunkResolution + 1);
        Mesh centerCrossMesh = MeshGenerators.CreateCrossMesh(chunkResolution * 4 - 1);

        _chunkMaterial = new Material(Shader.Find("Custom/ChunkShader"));
        _chunkMaterial.SetFloat("_TileSize", 4f * chunkResolution - 1);
        _chunkMaterial.SetFloat("_MaxHeight", maxHeight);
        Material material = _chunkMaterial;

        TerrainData terrainData = new TerrainData
        {
            ChunkResolution = chunkResolution,
            MaxHeight = maxHeight,
            Parent = transform,
            PlayerTransform = player,
            SquareChunkData = new ChunkPieceInfo { Mesh = squareChunkMesh, Material = material },
            BorderVerticalData = new ChunkPieceInfo { Mesh = borderVerticalMesh, Material = material },
            BorderHorizontalData = new ChunkPieceInfo { Mesh = borderHorizontalMesh, Material = material },
            InteriorVerticalData = new ChunkPieceInfo { Mesh = interiorVerticalMesh, Material = material },
            InteriorHorizontalData = new ChunkPieceInfo { Mesh = interiorHorizontalMesh, Material = material },
            CenterCrossData = new ChunkPieceInfo { Mesh = centerCrossMesh, Material = material },
            NoiseScale = noiseScale,
            NoiseOffset = noiseOffset
        };

        Shader heightmapShader = Shader.Find("Custom/HeightmapShader");

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

        _terrainCenter = new TerrainCenter(terrainData, heightmapShader, _terrainLevels[0].Heightmap);
    }

    void Update()
    {
        _chunkMaterial.SetFloat("_MaxHeight", maxHeight);
        _chunkMaterial.SetFloat("_DebugBlend", debugBlendVisualization ? 1f : 0f);

        _terrainCenter.UpdateNoiseParameters(maxHeight, noiseScale, noiseOffset);
        _terrainCenter.UpdateChunkPositions(player.position);

        for (int i = 0; i < numberOfLevels; i++)
        {
            _terrainLevels[i].UpdateNoiseParameters(maxHeight, noiseScale, noiseOffset);
            _terrainLevels[i].UpdateChunkPositions(player.position);
        }
    }
}
