using System.Collections.Generic;
using Open3DViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using Open3DViewer.Gui.PBRRenderEngine.Shaders;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using Open3DViewer.RenderViewControl;
using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine.GLTF
{
    public class GLTFMesh : RenderMesh<PBRRenderEngine, VertexLayoutFull, ObjectShader>
    {
        private ResourceLayout m_worldLayout;

        private readonly Dictionary<uint, Texture> m_textures = new Dictionary<uint, Texture>();

        private TextureView m_surfaceTextureView;
        
        public GLTFMesh(PBRRenderEngine engine) : base(engine)
        {
        }

        public void SetWorldBuffer(IRenderEngine engine, DeviceBuffer worldBuffer)
        {
            if (m_surfaceTextureView != null)
            {
                var worldSet = engine.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    m_worldLayout,
                    worldBuffer,
                    m_surfaceTextureView,
                    engine.GraphicsDevice.Aniso4xSampler));
                RegisterGraphicsResource(1, worldSet);
            }
            else
            {
                var worldSet = engine.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    m_worldLayout,
                    worldBuffer));
                RegisterGraphicsResource(1, worldSet);
            }
        }

        public void SetTexture(uint samplerIndex, Texture texture)
        {
            m_textures[samplerIndex] = texture;
        }

        public override void Initialize(VertexLayoutFull[] vertices, ushort[] indices)
        {
            if (m_textures.TryGetValue(0, out var data))
            {
                m_surfaceTextureView = m_engine.ResourceFactory.CreateTextureView(data);
            }
            
            base.Initialize(vertices, indices);
        }

        protected override Pipeline CreatePipeline(PBRRenderEngine engine, ObjectShader shader)
        {
            var factory = engine.ResourceFactory;
            
            var projViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                )
            );

            if (m_surfaceTextureView != null)
            {
                m_worldLayout = factory.CreateResourceLayout(
                    new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer,
                            ShaderStages.Vertex),
                        new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly,
                            ShaderStages.Fragment),
                        new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler,
                            ShaderStages.Fragment)
                    )
                );
            }
            else
            {
                m_worldLayout = factory.CreateResourceLayout(
                    new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                    )
                );                
            }

            var pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = new DepthStencilStateDescription
                (
                    depthTestEnabled: true,
                    depthWriteEnabled: true,
                    comparisonKind: ComparisonKind.LessEqual
                ),
                RasterizerState = new RasterizerStateDescription
                (
                    cullMode: FaceCullMode.Front,
                    fillMode: PolygonFillMode.Solid,
                    frontFace: FrontFace.Clockwise,
                    depthClipEnabled: true,
                    scissorTestEnabled: false
                ),
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = new[] { projViewLayout, m_worldLayout },
                ShaderSet = new ShaderSetDescription
                (
                    vertexLayouts: new [] { shader.GetVertexLayout() },
                    shaders: m_shaders
                ),
                Outputs = engine.Swapchain.Framebuffer.OutputDescription
            };

            var projViewSet = factory.CreateResourceSet(new ResourceSetDescription(
                projViewLayout,
                engine.GetSharedResource(CoreSharedResource.ProjectionBuffer),
                engine.GetSharedResource(CoreSharedResource.ViewBuffer)));
            RegisterGraphicsResource(0, projViewSet);
            
            return factory.CreateGraphicsPipeline(pipelineDescription);
        }
    }
}