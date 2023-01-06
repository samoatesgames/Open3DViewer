using System.Numerics;
using System.Runtime.InteropServices;

namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialInfo
    {
        public Vector4 Tint;
        public Vector2 MetallicRoughnessFactors;
        public uint BoundTextureBitMask;

        public uint MaterialInfo_Padding0;

        public static MaterialInfo Create()
        {
            return new MaterialInfo
            {
                Tint = Vector4.One,
                MetallicRoughnessFactors = Vector2.Zero,
                BoundTextureBitMask = 0,
                MaterialInfo_Padding0 = 0,
            };
        }
    }
}
