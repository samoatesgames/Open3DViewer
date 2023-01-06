using System.Numerics;
using System.Runtime.InteropServices;

namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SceneInfo
    {
        public Vector3 AmbientLightColor;
        public float Padding0;

        public Vector3 DirectionalLightDirection;
        public float Padding1;

        public Vector3 DirectionalLightColor;
        public float Padding2;

        public Vector3 CameraPosition;
        public float Padding3;

        public ShadingModes ShadingMode;
        public uint Padding4;
        public uint Padding5;
        public uint Padding6;

        public static SceneInfo Create()
        {
            return new SceneInfo
            {
                AmbientLightColor = new Vector3(0.7f, 0.7f, 0.7f),
                Padding0 = 0.0f,
                DirectionalLightDirection = Vector3.Normalize(new Vector3(-2, 1, -4)),
                Padding1 = 0.0f,
                DirectionalLightColor = new Vector3(0.7f, 0.7f, 0.7f),
                Padding2 = 0.0f,
                CameraPosition = Vector3.UnitZ,
                Padding3 = 0.0f,
                ShadingMode = ShadingModes.Default
            };
        }
    }
}