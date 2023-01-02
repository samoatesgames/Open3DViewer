using Open3DViewer.Gui.PBRRenderEngine.Camera;
using Open3DViewer.Gui.PBRRenderEngine.GLTF;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using Open3DViewer.RenderViewControl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine
{
    public class PBRRenderEngine : IRenderEngine
    {
        private readonly Dictionary<CoreSharedResource, BindableResource> m_coreSharedResources =
            new Dictionary<CoreSharedResource, BindableResource>();

        private PerspectiveCamera m_camera;

        private readonly long m_fixedUpdateTickCount = TimeSpan.FromMilliseconds(50).Ticks;
        private long m_lastUpdate;
        
        private GLTFEntity m_entity;
        public GLTFScene ActiveScene => m_entity?.Scene;

        public TextureResourceManager TextureResourceManager { get; private set; }
        public ShaderResourceManager ShaderResourceManager { get; private set; }
        public PerspectiveCamera Camera => m_camera;

        public GraphicsDevice GraphicsDevice { get; private set; }
        public ResourceFactory ResourceFactory { get; private set; }
        public Swapchain Swapchain { get; private set; }

        public RgbaFloat ClearColor { get; } = new RgbaFloat(0.45882f, 0.45882f, 0.45882f, 1.0f);
        public float ClearDepth { get; } = 1.0f;

        public delegate void RenderEngineInitialized(PBRRenderEngine engine);
        public event RenderEngineInitialized OnInitialized;

        public void Initialize(GraphicsDevice graphicsDevice, ResourceFactory factory, Swapchain swapchain)
        {
            GraphicsDevice = graphicsDevice;
            ResourceFactory = factory;
            Swapchain = swapchain;

            TextureResourceManager = new TextureResourceManager(this);
            ShaderResourceManager = new ShaderResourceManager(this);

            m_camera = new PerspectiveCamera(this, factory);

            OnInitialized?.Invoke(this);
        }

        public void Dispose()
        {
            m_entity?.Dispose();
            TextureResourceManager.Dispose();
            ShaderResourceManager.Dispose();
        }
        
        public void Render(CommandList commandList)
        {
            // TODO: Do a real fixed update as 'Render' is based on frame rate
            var now = DateTime.Now.Ticks;
            if (now - m_lastUpdate > m_fixedUpdateTickCount)
            {
                m_camera.FixedUpdate();
                m_lastUpdate = now;
            }

            m_camera.GenerateCommands(commandList);
            m_entity?.Render(commandList);
        }
        
        public void OnSwapchainResized(uint width, uint height)
        {
            m_camera.OnSwapchainResized(width, height);
        }

        public void OnMouseDown(RenderViewControl.RenderViewControl control, MouseButtonEventArgs args)
        {
            m_camera.OnMouseDown(control, args);
        }

        public void OnMouseUp(RenderViewControl.RenderViewControl control, MouseButtonEventArgs args)
        {
            m_camera.OnMouseUp(control, args);
        }

        public void OnMouseMove(RenderViewControl.RenderViewControl control, MouseEventArgs args)
        {
            m_camera.OnMouseMove(control, args);
        }

        public void OnMouseWheel(RenderViewControl.RenderViewControl sender, MouseWheelEventArgs args)
        {
            m_camera.OnMouseWheel(sender, args);
        }

        public void OnKeyDown(RenderViewControl.RenderViewControl control, KeyEventArgs args)
        {
            m_camera.OnKeyDown(control, args);
        }

        public void OnKeyUp(RenderViewControl.RenderViewControl control, KeyEventArgs args)
        {
            m_camera.OnKeyUp(control, args);
        }

        public void RegisterSharedResource(CoreSharedResource resourceType, BindableResource resource)
        {
            m_coreSharedResources[resourceType] = resource;
        }

        public async Task<bool> TryLoadAssetAsync(string assetPath)
        {
            if (m_entity != null)
            {
                m_camera.LookAt(null);
                m_entity.Dispose();
                m_entity = null;
            }

            GLTFScene gltfScene = null;
            await Task.Run(() =>
            {
                if (!GLTFScene.TryLoad(this, assetPath, out gltfScene))
                {
                    gltfScene = null;
                }
            });

            if (gltfScene == null)
            {
                return false;
            }

            m_entity = new GLTFEntity(gltfScene);
            m_camera.LookAt(m_entity);
            return true;
        }

        public BindableResource GetSharedResource(CoreSharedResource resourceType)
        {
            if (!m_coreSharedResources.TryGetValue(resourceType, out var resource))
            {
                return null;
            }
            return resource;
        }
    }
}