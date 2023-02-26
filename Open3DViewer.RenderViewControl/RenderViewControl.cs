using Open3DViewer.RenderViewControl.Types;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Veldrid;
using Window = System.Windows.Window;

namespace Open3DViewer.RenderViewControl
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

            MouseDown += (sender, args) =>
            {
                if (sender is RenderViewControl renderView)
                {
                    renderView.CaptureMouse();
                    RenderEngine?.OnMouseDown(CreateMouseButtonInfo(renderView, args));
                }
            };
            MouseUp += (sender, args) =>
            {
                if (sender is RenderViewControl renderView)
                {
                    RenderEngine?.OnMouseUp(CreateMouseButtonInfo(renderView, args));
                    renderView.ReleaseMouseCapture();
                }
            };
            MouseMove += (sender, args) =>
            {
                if (sender is RenderViewControl renderView)
                {
                    RenderEngine?.OnMouseMove(CreateMouseMoveInfo(renderView, args));
                }
            };
            MouseWheel += (sender, args) =>
            {
                if (sender is RenderViewControl)
                {
                    RenderEngine?.OnMouseWheel(CreateMouseWheelInfo(args));
                }
            };

            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.KeyDown += (sender, args) =>
                {
                    if (IsMouseOverControl())
                    {
                        RenderEngine?.OnKeyDown(CreateKeyPressInfo(args));
                    }
                };
                window.KeyUp += (sender, args) =>
                {
                    if (IsMouseOverControl())
                    {
                        RenderEngine?.OnKeyUp(CreateKeyPressInfo(args));
                    }
                };
            }
        }
        
        private KeyPressInfo CreateKeyPressInfo(KeyEventArgs args)
        {
            var offset = args.Key - Key.A;
            if (offset >= 0 && offset <= (Key.Z - Key.A))
            {
                var alphaKey = (char)('a' + offset);
                return new KeyPressInfo(alphaKey);
            }

            offset = args.Key - Key.D0;
            if (offset >= 0 && offset <= (Key.D9 - Key.D0))
            {
                var numericKey = (char)('0' + offset);
                return new KeyPressInfo(numericKey);
            }

            if (KeyPressInfo.KeyMap.TryGetValue(args.Key, out var mappedKey))
            {
                return new KeyPressInfo(mappedKey);
            }
            
            return new KeyPressInfo('\0');
        }

        private MouseWheelInfo CreateMouseWheelInfo(MouseWheelEventArgs args)
        {
            return new MouseWheelInfo(args.Delta);
        }

        private MouseMoveInfo CreateMouseMoveInfo(RenderViewControl renderView, MouseEventArgs args)
        {
            var position = args.GetPosition(renderView);
            return new MouseMoveInfo(new Vector2((float)position.X, (float)position.Y));
        }

        private MouseButtonInfo CreateMouseButtonInfo(RenderViewControl renderView, MouseButtonEventArgs args)
        {
            var pressedButton = args.ChangedButton == MouseButton.Left
                ? PressedMouseButton.Left 
                : args.ChangedButton == MouseButton.Right 
                    ? PressedMouseButton.Right
                    : PressedMouseButton.Middle;

            var position = args.GetPosition(renderView);
            return new MouseButtonInfo(pressedButton, new Vector2((float)position.X, (float)position.Y));
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

        [SupportedOSPlatform("windows")]
        public MemoryStream TakeScreenshot()
        {
            var position = PointToScreen(new System.Windows.Point(0d, 0d));
            using (var bmp = new Bitmap((int)m_swapchain.Framebuffer.Width, (int)m_swapchain.Framebuffer.Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen((int)position.X, (int)position.Y, 0, 0, bmp.Size);
                }

                var stream = new MemoryStream();
                bmp.Save(stream, ImageFormat.Png);
                stream.Position = 0;
                return stream;
            }
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

            RenderEngine?.OnSwapchainResized(width, height);
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