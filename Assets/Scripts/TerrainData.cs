using UnityEngine;

public partial class GPUClipmapTerrain
{
    class TerrainData
    {
        public int ChunkResolution;
        public float MaxHeight;
        public Transform Parent;
        public Transform PlayerTransform;
        public ChunkPieceInfo SquareChunkData;
        public ChunkPieceInfo BorderVerticalData;
        public ChunkPieceInfo BorderHorizontalData;
        public ChunkPieceInfo InteriorVerticalData;
        public ChunkPieceInfo InteriorHorizontalData;
        public ChunkPieceInfo CenterCrossData;
        public float NoiseScale;
        public Vector2 NoiseOffset;
        public int Octaves;
        public float Lacunarity;
        public float Persistence;
    }
}
