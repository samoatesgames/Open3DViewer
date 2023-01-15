using System;

namespace Open3DViewer.PBRRenderer.Types
{
    public enum ShadingModes : uint
    {
        [ShadingMode("Default", "Final Render")]
        Default = 0,

        // Maps
        [ShadingMode("Textures", "Diffuse Map Only")]
        MapOnlyDiffuse = 1,

        [ShadingMode("Textures", "Normal Map Only")]
        MapOnlyNormal = 2,

        [ShadingMode("Textures", "Metallic Map Only")]
        MapOnlyMetallic = 3,

        [ShadingMode("Textures", "Roughness Map Only")]
        MapOnlyRoughness = 4,

        [ShadingMode("Textures", "Occlusion Map Only")]
        MapOnlyOcclusion = 5,

        [ShadingMode("Textures", "Emissive Map Only")]
        MapOnlyEmissive = 6,

        // Lighting
        [ShadingMode("Lighting", "Direct Lighting Only")]
        LightingDirect = 7,

        [ShadingMode("Lighting", "Ambient Lighting Only")]
        LightingAmbient = 8,

        // Vertex
        [ShadingMode("Mesh", "Vertex Normals")]
        VertexNormal = 9,

        [ShadingMode("Mesh", "Vertex Tangents")]
        VertexTangent = 10,

        [ShadingMode("Mesh", "Vertex BiTangents")]
        VertexBiTangent = 11,

        [ShadingMode("Mesh", "Vertex Texture Coordinates")]
        VertexTexCoord = 12,

        [ShadingMode("Mesh", "Vertex Position")]
        VertexPosition = 13,
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
