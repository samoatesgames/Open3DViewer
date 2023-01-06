namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    public enum ShadingModes : uint
    {
        Default = 0,

        // Maps
        MapOnly_Diffuse = 1,
        MapOnly_Normal = 2,
        MapOnly_Metallic = 3,
        MapOnly_Roughness = 4,
        MapOnly_Emissive = 5,
        MapOnly_Occlusion = 6,

        // Lighting
        Lighting_Direct = 7,
        Lighting_Ambient = 8,

        // Vertex
        Vertex_Normal = 9,
        Vertex_Tangent = 10,
        Vertex_BiTangent = 11,
        Vertex_TexCoord = 12,
    }
}
