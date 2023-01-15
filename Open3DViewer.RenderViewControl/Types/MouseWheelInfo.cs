namespace Open3DViewer.RenderViewControl.Types
{
    public class MouseWheelInfo
    {
        public float WheelAmount { get; }

        public MouseWheelInfo(float wheelAmount)
        {
            WheelAmount = wheelAmount;
        }
    }
}
