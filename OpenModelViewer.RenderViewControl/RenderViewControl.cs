﻿using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Veldrid;

namespace OpenModelViewer.RenderViewControl
{
    public sealed class RenderViewControl : Win32HwndControl
    {
        private Swapchain m_swapchain;
        private CommandList m_commandList;
        private GraphicsDevice m_graphicsDevice;
        private bool m_rendering;
        
        public static readonly DependencyProperty RenderEngineProperty = 
            DependencyProperty.Register(nameof(RenderEngine), typeof(IRenderEngine), typeof(RenderViewControl)); 

        public IRenderEngine RenderEngine
        { 
            get => (IRenderEngine)GetValue(RenderEngineProperty);
            set => SetValue(RenderEngineProperty, value);
        } 

        protected override void Initialize()
        {
            m_graphicsDevice = GraphicsDevice.CreateD3D11(new GraphicsDeviceOptions());
            var factory = m_graphicsDevice.ResourceFactory;
            m_commandList = factory.CreateCommandList();
            m_swapchain = CreateSwapchain(factory);

            RenderEngine?.Initialize(m_graphicsDevice, factory, m_swapchain);

            m_rendering = true;
            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        protected override void Uninitialize()
        {
            m_rendering = false;
            CompositionTarget.Rendering -= OnCompositionTargetRendering;
            RenderEngine?.Dispose();
            DestroySwapchain();
        }

        protected override void Resized()
        {
            ResizeSwapchain();
        }

        private void OnCompositionTargetRendering(object sender, EventArgs eventArgs)
        {
            if (!m_rendering)
            {
                return;
            }

            if (RenderEngine == null)
            {
                return;
            }

            Render();
        }

        private double GetDpiScale()
        {
            var source = PresentationSource.FromVisual(this);
            return source?.CompositionTarget == null 
                ? default 
                : source.CompositionTarget.TransformToDevice.M11;
        }

        private Swapchain CreateSwapchain(ResourceFactory factory)
        {
            var dpiScale = GetDpiScale();
            var width = (uint)(ActualWidth < 0 ? 0 : Math.Ceiling(ActualWidth * dpiScale));
            var height = (uint)(ActualHeight < 0 ? 0 : Math.Ceiling(ActualHeight * dpiScale));

            var mainModule = typeof(RenderViewControl).Module;
            var hInstance = Marshal.GetHINSTANCE(mainModule);
            var win32Source = SwapchainSource.CreateWin32(Hwnd, hInstance);
            var scDesc = new SwapchainDescription(win32Source, width, height, Veldrid.PixelFormat.R32_Float, true);

            return factory.CreateSwapchain(scDesc);
        }

        private void DestroySwapchain()
        {
            m_swapchain.Dispose();
        }

        private void ResizeSwapchain()
        {
            var dpiScale = GetDpiScale();
            var width = (uint)(ActualWidth < 0 ? 0 : Math.Ceiling(ActualWidth * dpiScale));
            var height = (uint)(ActualHeight < 0 ? 0 : Math.Ceiling(ActualHeight * dpiScale));
            m_swapchain.Resize(width, height);
        }

        private void Render()
        {
            m_commandList.Begin();
            m_commandList.SetFramebuffer(m_swapchain.Framebuffer);

            m_commandList.ClearColorTarget(0, RenderEngine.ClearColor);
            m_commandList.ClearDepthStencil(RenderEngine.ClearDepth);

            // Call our render engines render
            RenderEngine.Render(m_commandList);
            
            m_commandList.End();
            m_graphicsDevice.SubmitCommands(m_commandList);
            m_graphicsDevice.SwapBuffers(m_swapchain);
        }
    }
}