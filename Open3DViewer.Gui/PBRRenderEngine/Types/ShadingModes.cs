namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    public enum ShadingModes : uint
    {
        Default = 0,

        // Maps
        MapOnly_Diffuse = 1,
        MapOnly_Normal = 2,

        // Lighting
        Lighting_Diffuse = 3,
        Lighting_Specular = 4,

        // Vertex
        Vertex_Normal = 5,
        Vertex_TexCoord = 6
    }
}
