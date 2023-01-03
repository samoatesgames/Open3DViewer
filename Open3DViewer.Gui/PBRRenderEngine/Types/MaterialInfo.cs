using System.Numerics;
using System.Runtime.InteropServices;

namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialInfo
    {
        public Vector4 Tint;
        
        public static MaterialInfo Create()
        {
            return new MaterialInfo
            {
                Tint = Vector4.One
            };
        }
    }
}
