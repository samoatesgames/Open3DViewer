using System;

namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    public enum ShadingModes : uint
    {
        [ShadingMode("Default", "Final Render")]
        Default = 0,

        // Maps
        [ShadingMode("Textures", "Diffuse Map Only")]
        MapOnly_Diffuse = 1,

        [ShadingMode("Textures", "Normal Map Only")]
        MapOnly_Normal = 2,

        [ShadingMode("Textures", "Metallic Map Only")]
        MapOnly_Metallic = 3,

        [ShadingMode("Textures", "Roughness Map Only")]
        MapOnly_Roughness = 4,

        [ShadingMode("Textures", "Occlusion Map Only")]
        MapOnly_Occlusion = 5,

        [ShadingMode("Textures", "Emissive Map Only")]
        MapOnly_Emissive = 6,

        // Lighting
        [ShadingMode("Lighting", "Direct Lighting Only")]
        Lighting_Direct = 7,

        [ShadingMode("Lighting", "Ambient Lighting Only")]
        Lighting_Ambient = 8,

        // Vertex
        [ShadingMode("Mesh", "Vertex Normals")]
        Vertex_Normal = 9,

        [ShadingMode("Mesh", "Vertex Tangents")]
        Vertex_Tangent = 10,

        [ShadingMode("Mesh", "Vertex BiTangents")]
        Vertex_BiTangent = 11,

        [ShadingMode("Mesh", "Vertex Texture Coordinates")]
        Vertex_TexCoord = 12,

        [ShadingMode("Mesh", "Vertex Position")]
        Vertex_Position = 13,
    }

    [AttributeUsage(AttributeTargets.All)]
    public class ShadingModeAttribute : Attribute
    {
        public string Group { get; }
        public string Description { get; }

        public ShadingModeAttribute(string group, string description)
        {
            Group = group;
            Description = description;
        }
    }
}
