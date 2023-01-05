namespace Open3DViewer.Gui.PBRRenderEngine.Buffers.Vertex
{
    public struct VertexLayoutFull : IVertexLayout
    {
        public float PositionX;
        public float PositionY;
        public float PositionZ;

        public float NormalX;
        public float NormalY;
        public float NormalZ;

        public float TangentX;
        public float TangentY;
        public float TangentZ;

        public float BiTangentX;
        public float BiTangentY;
        public float BiTangentZ;

        public float TexU0;
        public float TexV0;
        
        public uint GetSizeInBytes() => 56;
    }
}