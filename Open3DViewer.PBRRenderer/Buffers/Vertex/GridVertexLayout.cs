using System.Numerics;

namespace Open3DViewer.PBRRenderer.Buffers.Vertex
{
    public struct GridVertexLayout : IVertexLayout
    {
        public float PositionX;
        public float PositionY;
        public float PositionZ;

        public float ColorR;
        public float ColorG;
        public float ColorB;

        public uint GetSizeInBytes() => 24;

        public GridVertexLayout(float x, float y, float z, Vector3 color)
        {
            PositionX = x;
            PositionY = y;
            PositionZ = z;

            ColorR = color.X;
            ColorG = color.Y;
            ColorB = color.Z;
        }
    }
}
