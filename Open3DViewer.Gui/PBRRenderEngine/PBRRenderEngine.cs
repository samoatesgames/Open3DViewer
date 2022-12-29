using System.Collections.Generic;
using System.Threading.Tasks;
using Open3DViewer.Gui.PBRRenderEngine.Camera;
using Open3DViewer.Gui.PBRRenderEngine.GLTF;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using Open3DViewer.RenderViewControl;
using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine
{
    public class PBRRenderEngine : IRenderEngine
    {
        private readonly Dictionary<CoreSharedResource, BindableResource> m_coreSharedResources =
            new Dictionary<CoreSharedResource, BindableResource>();

        private PerspectiveCamera m_camera;
        
        private GLTFEntity<PBRRenderEngine> m_entity;
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

            m_camera = new PerspectiveCamera(this, factory);

            OnInitialized?.Invoke(this);
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

            m_entity = new GLTFEntity<PBRRenderEngine>(this, gltfScene);
            m_camera.LookAt(m_entity);
            return true;
        }

        public void Render(CommandList commandList)
        {
            m_camera.GenerateCommands(commandList);
            m_entity?.Render(commandList);
        }

        public void OnSwapchainResized(uint width, uint height)
        {
            m_camera.OnSwapchainResized(width, height);
        }

        public void Dispose()
        {
            m_entity?.Dispose();
        }

        public void RegisterSharedResource(CoreSharedResource resourceType, BindableResource resource)
        {
            m_coreSharedResources[resourceType] = resource;
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