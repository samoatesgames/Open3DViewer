using Open3DViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using Open3DViewer.Gui.PBRRenderEngine.Shaders;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.SPIRV;

namespace Open3DViewer.Gui.PBRRenderEngine.GLTF
{
    public class GLTFMesh : IDisposable
    {
        private readonly PBRRenderEngine m_engine;
        private readonly Dictionary<uint, ResourceSet> m_graphicsResources = new Dictionary<uint, ResourceSet>();
        private readonly Dictionary<uint, Texture> m_textures = new Dictionary<uint, Texture>();
        private readonly DeviceBuffer m_worldBuffer;
        private readonly Matrix4x4 m_localTransform;

        private Shader[] m_shaders;
        private Pipeline m_pipeline;
        
        private DeviceBuffer m_vertexBuffer;
        private DeviceBuffer m_indexBuffer;
        private uint m_indexCount;
        
        private TextureView m_surfaceTextureView;

        public GLTFMesh(PBRRenderEngine engine, Matrix4x4 localTransform)
        {
            m_engine = engine;
            m_worldBuffer = engine.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            m_localTransform = localTransform;
        }

        public void Dispose()
        {
            m_vertexBuffer.Dispose();
            m_indexBuffer.Dispose();
            m_worldBuffer.Dispose();
            m_surfaceTextureView.Dispose();

            m_pipeline.Dispose();
            foreach (var shader in m_shaders)
            {
                shader.Dispose();
            }
        }

        public void SetTexture(uint samplerIndex, Texture texture)
        {
            m_textures[samplerIndex] = texture;
        }

        public void Initialize(VertexLayoutFull[] vertices, ushort[] indices)
        {
            if (m_textures.TryGetValue(0, out var data))
            {
                m_surfaceTextureView = m_engine.ResourceFactory.CreateTextureView(data);
            }

            var sizeInByes = vertices[0].GetSizeInBytes();
            m_indexCount = (uint)indices.Length;

            m_vertexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(vertices.Length * sizeInByes), BufferUsage.VertexBuffer));
            m_indexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));

            m_engine.GraphicsDevice.UpdateBuffer(m_vertexBuffer, 0, vertices);
            m_engine.GraphicsDevice.UpdateBuffer(m_indexBuffer, 0, indices);

            var shader = new ObjectShader();
            m_shaders = CreateShaders(m_engine.ResourceFactory, shader);
            m_pipeline = CreatePipeline(m_engine, shader);
        }

        public virtual void Render(CommandList commandList, Matrix4x4 worldTransform)
        {
            var transform = m_localTransform * worldTransform;
            commandList.UpdateBuffer(m_worldBuffer, 0, ref transform);

            commandList.SetVertexBuffer(0, m_vertexBuffer);
            commandList.SetIndexBuffer(m_indexBuffer, IndexFormat.UInt16);
            commandList.SetPipeline(m_pipeline);

            foreach (var entry in m_graphicsResources)
            {
                commandList.SetGraphicsResourceSet(entry.Key, entry.Value);
            }

            commandList.DrawIndexed(m_indexCount, 1, 0, 0, 0);
        }

        private Shader[] CreateShaders(ResourceFactory factory, ObjectShader shader)
        {
            var vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                shader.GetVertexShader(),
                "main");
            var fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                shader.GetPixelShader(),
                "main");

            return factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
        }

        private Pipeline CreatePipeline(PBRRenderEngine engine, ObjectShader shader)
        {
            var factory = engine.ResourceFactory;
            
            var projViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                )
            );

            var worldLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                )
            );

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
                ResourceLayouts = new[] { projViewLayout, worldLayout },
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

            var worldSet = engine.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                worldLayout,
                m_worldBuffer,
                m_surfaceTextureView,
                engine.GraphicsDevice.Aniso4xSampler));
            RegisterGraphicsResource(1, worldSet);

            return factory.CreateGraphicsPipeline(pipelineDescription);
        }

        private void RegisterGraphicsResource(uint slot, ResourceSet resourceSet)
        {
            m_graphicsResources[slot] = resourceSet;
        }
    }
}