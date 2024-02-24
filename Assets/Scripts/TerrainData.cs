using UnityEngine;

public partial class GPUClipmapTerrain
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
}
