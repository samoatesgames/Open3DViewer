using System.Numerics;
using System.Runtime.InteropServices;

namespace Open3DViewer.PBRRenderer.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ViewProjectionInfo
    {
        public Matrix4x4 View;
        public Matrix4x4 Projection;
    }
}
