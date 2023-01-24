using System.Numerics;
using System.Runtime.InteropServices;

namespace Open3DViewer.PBRRenderer.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialInfo
    {
        public Vector4 Tint;
        public Vector2 MetallicRoughnessFactors;
        public uint BoundTextureBitMask;
        public float OcclusionStrength;
        public Vector4 EmissiveFactors;
        public uint AlphaMode;
        public float AlphaCutoff;

        public Vector2 Padding;

        public static MaterialInfo Create()
        {
            return new MaterialInfo
            {
                Tint = Vector4.One,
                MetallicRoughnessFactors = Vector2.Zero,
                BoundTextureBitMask = 0,
                OcclusionStrength = 1,
                EmissiveFactors = new Vector4(0, 0, 0, 1),
                AlphaMode = 0,
                AlphaCutoff = 0.5f
            };
        }
    }
}
