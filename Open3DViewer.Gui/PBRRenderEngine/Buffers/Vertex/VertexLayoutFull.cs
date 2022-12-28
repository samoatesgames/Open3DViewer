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

        public float TexU0;
        public float TexV0;
        
        public uint GetSizeInBytes() => 32;
    }
}