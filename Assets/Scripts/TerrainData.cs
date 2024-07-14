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
    }
}
