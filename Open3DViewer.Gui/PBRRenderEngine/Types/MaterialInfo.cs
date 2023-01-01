using System.Numerics;
using System.Runtime.InteropServices;

namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialInfo
    {
        public Vector4 Tint;

        public Vector3 AmbientLightColor;
        public float Padding0;

        public Vector3 DirectionalLightDirection;
        public float Padding1;

        public Vector3 DirectionalLightColor;
        public float Padding2;

        public static MaterialInfo Create()
        {
            return new MaterialInfo
            {
                Tint = Vector4.One,
                AmbientLightColor = new Vector3(0.8f, 0.7f, 0.7f),
                Padding0 = 0.0f,
                DirectionalLightDirection = Vector3.Normalize(new Vector3(-2, 1, -4)),
                Padding1 = 0.0f,
                DirectionalLightColor = Vector3.One,
                Padding2 = 0.0f
            };
        }
    }
}
