using System;
using System.Windows.Input;
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

        void OnMouseDown(RenderViewControl control, MouseButtonEventArgs args);
        void OnMouseUp(RenderViewControl control, MouseButtonEventArgs args);
        void OnMouseMove(RenderViewControl control, MouseEventArgs args);
        void OnMouseWheel(RenderViewControl sender, MouseWheelEventArgs args);

        void OnKeyDown(RenderViewControl control, KeyEventArgs args);
        void OnKeyUp(RenderViewControl control, KeyEventArgs args);
    }
}