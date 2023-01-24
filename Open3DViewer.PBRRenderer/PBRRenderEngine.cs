using Open3DViewer.PBRRenderer.Camera;
using Open3DViewer.PBRRenderer.GLTF;
using Open3DViewer.PBRRenderer.Meshes;
using Open3DViewer.PBRRenderer.Types;
using Open3DViewer.RenderViewControl;
using Open3DViewer.RenderViewControl.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Veldrid;
using Vortice.Mathematics;

namespace Open3DViewer.PBRRenderer
{
    public class PBRRenderEngine : IRenderEngine
    {
        private readonly Dictionary<CoreSharedResource, BindableResource> m_coreSharedResources =
            new Dictionary<CoreSharedResource, BindableResource>();

        private SceneInfo m_sceneInfo = SceneInfo.Create();
        private DeviceBuffer m_sceneInfoBuffer;

        private bool m_gridVisible;
        private GridMesh m_grid;
        private PerspectiveCamera m_camera;
        private bool m_isAssetLoading;

        private readonly long m_fixedUpdateTickCount = TimeSpan.FromMilliseconds(50).Ticks;
        private long m_lastUpdate;
        
        private GLTFEntity m_entity;
        public GLTFScene ActiveScene => m_entity?.Scene;
        public bool IsGridVisible => m_gridVisible;

        public bool IsAssetLoading
        {
            get => m_isAssetLoading;
            private set
            {
                m_isAssetLoading = value;
                OnAssetLoadingChanged?.Invoke(this, value);
            }
        }

        public TextureResourceManager TextureResourceManager { get; private set; }
        public ShaderResourceManager ShaderResourceManager { get; private set; }
        public PerspectiveCamera Camera => m_camera;
        public SceneInfo SceneInfo => m_sceneInfo;
        public DeviceBuffer SceneInfoBuffer => m_sceneInfoBuffer;

        public GraphicsDevice GraphicsDevice { get; private set; }
        public ResourceFactory ResourceFactory { get; private set; }
        public Swapchain Swapchain { get; private set; }

        public RgbaFloat ClearColor { get; } = new RgbaFloat(0.45882f, 0.45882f, 0.45882f, 1.0f);
        public float ClearDepth { get; } = 1.0f;

        public delegate void RenderEngineInitialized(PBRRenderEngine engine);
        public event RenderEngineInitialized OnInitialized;

        public delegate void RenderEngineGridVisibilityChanged(PBRRenderEngine engine, bool isVisible);
        public event RenderEngineGridVisibilityChanged OnGridVisibilityChanged;

        public delegate Task RenderEngineAssetLoadingChanged(PBRRenderEngine engine, bool isLoading);
        public event RenderEngineAssetLoadingChanged OnAssetLoadingChanged;

        public void Initialize(GraphicsDevice graphicsDevice, ResourceFactory factory, Swapchain swapchain)
        {
            GraphicsDevice = graphicsDevice;
            ResourceFactory = factory;
            Swapchain = swapchain;

            TextureResourceManager = new TextureResourceManager(this);
            ShaderResourceManager = new ShaderResourceManager(this);

            m_camera = new PerspectiveCamera(this, factory);

            RecreateGrid();

            var bufferDescription = new BufferDescription(SceneInfo.ClassSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic);
            m_sceneInfoBuffer = ResourceFactory.CreateBuffer(bufferDescription);
            m_sceneInfoBuffer.Name = "SceneInfo_Buffer";

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

            m_sceneInfo.CameraPosition = m_camera.Position;
            commandList.UpdateBuffer(m_sceneInfoBuffer, 0, m_sceneInfo.ToSpan());

            if (m_gridVisible)
            {
                m_grid?.Render(commandList);
            }
            
            m_entity?.Render(commandList, m_camera, RenderPass.Opaque);
            m_entity?.Render(commandList, m_camera, RenderPass.Alpha);
        }
        
        public void OnSwapchainResized(uint width, uint height)
        {
            m_camera.OnSwapchainResized(width, height);
        }

        public void OnMouseDown(MouseButtonInfo args)
        {
            m_camera.OnMouseDown(args);
        }

        public void OnMouseUp(MouseButtonInfo args)
        {
            m_camera.OnMouseUp(args);
        }

        public void OnMouseMove(MouseMoveInfo args)
        {
            m_camera.OnMouseMove(args);
        }

        public void OnMouseWheel(MouseWheelInfo args)
        {
            m_camera.OnMouseWheel(args);
        }

        public void OnKeyDown(KeyPressInfo args)
        {
            m_camera.OnKeyDown(args);

            if (args.Key == 'g')
            {
                SetGridVisible(!m_gridVisible);
            }
        }

        public void OnKeyUp(KeyPressInfo args)
        {
            m_camera.OnKeyUp(args);
        }

        public void RegisterSharedResource(CoreSharedResource resourceType, BindableResource resource)
        {
            m_coreSharedResources[resourceType] = resource;
        }

        public void SetShadingMode(ShadingModes shadingMode)
        {
            m_sceneInfo.ShadingMode = shadingMode;
        }

        public void SetAmbientLightColor(Vector3 color)
        {
            m_sceneInfo.AmbientLightColor = color;
        }

        public void SetDirectionalLightColor(int lightIndex, Vector3 color)
        {
            m_sceneInfo.Lights[lightIndex].Radiance = color;
        }

        public void SetDirectionalLightActive(int lightIndex, bool isActive)
        {
            m_sceneInfo.Lights[lightIndex].IsActive = (uint)(isActive ? 1 : 0);
        }

        public async Task<bool> TryLoadAssetAsync(string assetPath)
        {
            IsAssetLoading = true;

            if (m_entity != null)
            {
                m_camera.LookAt(null);
                m_entity.Dispose();
                m_entity = null;
            }

            m_grid?.Dispose();
            m_grid = null;

            var gltfScene = await GLTFScene.TryLoad(this, assetPath);
            if (gltfScene == null)
            {
                RecreateGrid();
                IsAssetLoading = false;
                return false;
            }

            m_entity = new GLTFEntity(gltfScene);
            m_camera.LookAt(m_entity);
            RecreateGrid(m_entity.GetBoundingBox());
            IsAssetLoading = false;
            return true;
        }

        private void RecreateGrid()
        {
            RecreateGrid(new BoundingBox(Vector3.One * -2, Vector3.One * 2));
        }

        private void RecreateGrid(BoundingBox entityBounds)
        {
            m_grid?.Dispose();

            var size = Math.Max(entityBounds.Extent.X, entityBounds.Extent.Z);
            m_grid = new GridMesh(this, size * 2.0f, entityBounds.Minimum.Y);
        }

        public void SetGridVisible(bool visible)
        {
            if (m_gridVisible == visible)
            {
                return;
            }

            m_gridVisible = visible;
            OnGridVisibilityChanged?.Invoke(this, m_gridVisible);
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