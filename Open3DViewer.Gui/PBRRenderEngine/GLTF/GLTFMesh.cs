using Open3DViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using Open3DViewer.Gui.PBRRenderEngine.Shaders;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Vortice.Mathematics;

namespace Open3DViewer.Gui.PBRRenderEngine.GLTF
{
    public class GLTFMesh : IDisposable
    {
        private readonly PBRRenderEngine m_engine;
        private readonly Dictionary<uint, ResourceSet> m_graphicsResources = new Dictionary<uint, ResourceSet>();
        
        private readonly DeviceBuffer m_worldBuffer;
        private readonly Matrix4x4 m_localTransform;
        
        private Pipeline m_pipeline;
        
        private DeviceBuffer m_vertexBuffer;
        private DeviceBuffer m_indexBuffer;
        private uint m_indexCount;

        private readonly Dictionary<TextureSamplerIndex, TextureView> m_textureViews = new Dictionary<TextureSamplerIndex, TextureView>();
        
        public BoundingBox BoundingBox { get; } 

        public GLTFMesh(PBRRenderEngine engine, Matrix4x4 localTransform, BoundingBox boundingBox)
        {
            m_engine = engine;
            m_worldBuffer = engine.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            m_localTransform = localTransform;
            BoundingBox = boundingBox;
        }

        public void Dispose()
        {
            m_vertexBuffer.Dispose();
            m_indexBuffer.Dispose();
            m_worldBuffer.Dispose();

#if DEBUG
            m_engine.ShaderResourceManager.OnShaderReloaded -= OnShaderReloaded;
#endif

            m_pipeline.Dispose();
        }

        public void SetTexture(TextureSamplerIndex samplerIndex, TextureView textureView)
        {
            textureView.Name = $"TextureView_{samplerIndex}";
            m_textureViews[samplerIndex] = textureView;
        }

        public void Initialize(VertexLayoutFull[] vertices, ushort[] indices)
        {
            var sizeInByes = vertices[0].GetSizeInBytes();
            m_indexCount = (uint)indices.Length;

            m_vertexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(vertices.Length * sizeInByes), BufferUsage.VertexBuffer));
            m_indexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));

            m_engine.GraphicsDevice.UpdateBuffer(m_vertexBuffer, 0, vertices);
            m_engine.GraphicsDevice.UpdateBuffer(m_indexBuffer, 0, indices);
            
            m_pipeline = CreatePipeline<ObjectShader>(m_engine);

#if DEBUG
            m_engine.ShaderResourceManager.OnShaderReloaded += OnShaderReloaded;
#endif
        }

#if DEBUG
        private void OnShaderReloaded(IRenderShader shader)
        {
            if (!(shader is ObjectShader))
            {
                return;
            }
            m_pipeline = CreatePipeline<ObjectShader>(m_engine);
        }
#endif

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

        private Pipeline CreatePipeline<TShader>(PBRRenderEngine engine) where TShader : IRenderShader
        {
            var factory = engine.ResourceFactory;

            var resourceLayouts = new List<ResourceLayout>();

            var projViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                )
            );
            resourceLayouts.Add(projViewLayout);
            var projViewSet = factory.CreateResourceSet(new ResourceSetDescription(
                projViewLayout,
                engine.GetSharedResource(CoreSharedResource.ProjectionBuffer),
                engine.GetSharedResource(CoreSharedResource.ViewBuffer)));
            RegisterGraphicsResource(0, projViewSet);

            var worldLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                )
            );
            resourceLayouts.Add(worldLayout);
            var worldSet = engine.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                worldLayout,
                m_worldBuffer));
            RegisterGraphicsResource(1, worldSet);

            var resourceSet = 2u;
            foreach (TextureSamplerIndex samplerType in Enum.GetValues(typeof(TextureSamplerIndex)))
            {
                var textureLayout = factory.CreateResourceLayout(
                    new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription($"{samplerType}Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription($"{samplerType}Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
                    )
                );
                textureLayout.Name = $"TextureLayout_{samplerType}";
                resourceLayouts.Add(textureLayout);

                var currentSet = resourceSet;
                resourceSet++;

                if (!m_textureViews.TryGetValue(samplerType, out var textureView))
                {
                    textureView = m_engine.TextureResourceManager.GetFallbackTexture(samplerType);
                }

                var textureSet = engine.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    textureLayout,
                    engine.GraphicsDevice.Aniso4xSampler,
                    textureView));
                textureSet.Name = $"TextureSet_{samplerType}";
                RegisterGraphicsResource(currentSet, textureSet);
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
                ResourceLayouts = resourceLayouts.ToArray(),
                ShaderSet = m_engine.ShaderResourceManager.GetShaderSet<TShader>(),
                Outputs = engine.Swapchain.Framebuffer.OutputDescription
            };

            return factory.CreateGraphicsPipeline(pipelineDescription);
        }

        private void RegisterGraphicsResource(uint slot, ResourceSet resourceSet)
        {
            m_graphicsResources[slot] = resourceSet;
        }
    }
}