using Open3DViewer.RenderViewControl.Types;
using System;
using Veldrid;

namespace Open3DViewer.RenderViewControl
{
    public interface IRenderEngine : IDisposable
    {
        GraphicsDevice GraphicsDevice { get; }
        ResourceFactory ResourceFactory { get; }
        Swapchain Swapchain { get; }
        
        RgbaFloat ClearColor { get; }
        float ClearDepth { get; }
        
        void Initialize(GraphicsDevice graphicsDevice, ResourceFactory factory, Swapchain swapchain);
        void Render(CommandList commandList);

        void OnSwapchainResized(uint width, uint height);

        void OnMouseDown(MouseButtonInfo args);
        void OnMouseUp(MouseButtonInfo args);
        void OnMouseMove(MouseMoveInfo args);
        void OnMouseWheel(MouseWheelInfo args);

        void OnKeyDown(KeyPressInfo args);
        void OnKeyUp(KeyPressInfo args);
    }
}