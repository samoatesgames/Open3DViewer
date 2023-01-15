using System.Numerics;

namespace Open3DViewer.RenderViewControl.Types
{
    public enum PressedMouseButton
    {
        Left,
        Right,
        Middle
    }

    public class MouseButtonInfo
    {
        public PressedMouseButton PressedButton { get; }
        public Vector2 Position { get; }

        public MouseButtonInfo(PressedMouseButton pressedButton, Vector2 position)
        {
            PressedButton = pressedButton;
            Position = position;
        }
    }
}
