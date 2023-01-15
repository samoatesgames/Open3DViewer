using System.Numerics;

namespace Open3DViewer.RenderViewControl.Types
{
    public class MouseMoveInfo
    {
        public Vector2 Position { get; }

        public MouseMoveInfo(Vector2 position)
        {
            Position = position;
        }
    }
}
