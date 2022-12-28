using System;
using Veldrid;

namespace OpenModelViewer.RenderViewControl
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
    }
}