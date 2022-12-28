using System.Collections.Generic;
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
        public RgbaFloat ClearColor { get; } = RgbaFloat.Cyan;
        public float ClearDepth { get; } = 1.0f;
        
        public void Initialize(GraphicsDevice graphicsDevice, ResourceFactory factory, Swapchain swapchain)
        {
            GraphicsDevice = graphicsDevice;
            ResourceFactory = factory;
            Swapchain = swapchain;
            
            m_camera = new PerspectiveCamera(this, factory);

            //var gltfPath = @"Assets\Box\BoxTextured.glb";
            var gltfPath = @"Assets\Helmet\DamagedHelmet.glb";
            GLTFScene.TryLoad(this, gltfPath, out var gltfScene);
            m_entity = new GLTFEntity<PBRRenderEngine>(this, gltfScene);
        }

        public void Render(CommandList commandList)
        {
            m_camera.GenerateCommands(commandList);

            m_entity.Render(commandList);
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