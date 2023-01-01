using Open3DViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using Open3DViewer.Gui.PBRRenderEngine.Shaders;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
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

        private MaterialInfo m_materialInfo;
        private readonly DeviceBuffer m_materialInfoBuffer;

        public BoundingBox BoundingBox { get; } 

        public GLTFMesh(PBRRenderEngine engine, Matrix4x4 localTransform, BoundingBox boundingBox)
        {
            m_engine = engine;
            m_worldBuffer = engine.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            m_worldBuffer.Name = "World_Buffer";

            m_localTransform = localTransform;
            BoundingBox = boundingBox;

            m_materialInfo = MaterialInfo.Create();

            var bufferDescription = new BufferDescription((uint)Marshal.SizeOf<MaterialInfo>(), BufferUsage.UniformBuffer | BufferUsage.Dynamic);
            m_materialInfoBuffer = m_engine.ResourceFactory.CreateBuffer(bufferDescription);
            m_materialInfoBuffer.Name = "MaterialInfo_Buffer";
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

        public void SetDiffuseTint(Vector4 tintColor)
        {
            m_materialInfo.Tint = tintColor;
        }

        public void SetShadingMode(ShadingModes shadingMode)
        {
            m_materialInfo.ShadingMode = shadingMode;
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

            m_materialInfo.CameraPosition = m_engine.Camera.Position;
            commandList.UpdateBuffer(m_materialInfoBuffer, 0, ref m_materialInfo);

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

            // Camera Project/View resources
            CreateProjectionViewResources(engine, out var projViewLayout, out var projViewSet);
            resourceLayouts.Add(projViewLayout);
            RegisterGraphicsResource(0, projViewSet);

            // World matrix resources
            CreateWorldMatrixResources(engine, out var worldLayout, out var worldSet);
            resourceLayouts.Add(worldLayout);
            RegisterGraphicsResource(1, worldSet);
            
            // Material info resources
            CreateMaterialInfoResource(engine, out var materialInfoLayout, out var materialInfoSet);
            resourceLayouts.Add(materialInfoLayout);
            RegisterGraphicsResource(2, materialInfoSet);

            // Setup texture sampler resources
            foreach (TextureSamplerIndex samplerType in Enum.GetValues(typeof(TextureSamplerIndex)))
            {
                if (samplerType != TextureSamplerIndex.Diffuse && samplerType != TextureSamplerIndex.Normal)
                {
                    // TODO: Support more than the diffuse and normal
                    continue;
                }

                var textureLayout = factory.CreateResourceLayout(
                    new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription($"{samplerType}Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription($"{samplerType}Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)
                    )
                );
                textureLayout.Name = $"{samplerType}_TextureLayout";
                resourceLayouts.Add(textureLayout);

                if (!m_textureViews.TryGetValue(samplerType, out var textureView))
                {
                    textureView = m_engine.TextureResourceManager.GetFallbackTexture(samplerType);
                }
                
                var textureSet = engine.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    textureLayout,
                    engine.GraphicsDevice.LinearSampler,
                    textureView));
                textureSet.Name = $"{samplerType}_TextureSet";
                RegisterGraphicsResource((uint)samplerType + 3, textureSet);
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

        private void CreateWorldMatrixResources(PBRRenderEngine engine, out ResourceLayout worldLayout, out ResourceSet worldSet)
        {
            var factory = engine.ResourceFactory;

            worldLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                    {
                        Name = "WorldBuffer_LayoutDescription"
                    }
                )
            );

            worldSet = factory.CreateResourceSet(new ResourceSetDescription(
                worldLayout,
                m_worldBuffer));
            worldSet.Name = "World_ResourceSet";
        }

        private void CreateMaterialInfoResource(PBRRenderEngine engine, out ResourceLayout materialInfoLayout, out ResourceSet materialInfoSet)
        {
            var factory = engine.ResourceFactory;

            materialInfoLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("MaterialInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                    {
                        Name = "MaterialInfoBuffer_LayoutDescription"
                    }
                )
            );

            materialInfoSet = factory.CreateResourceSet(new ResourceSetDescription(
                materialInfoLayout,
                m_materialInfoBuffer));
            materialInfoSet.Name = "MaterialInfo_ResourceSet";
        }

        private void CreateProjectionViewResources(PBRRenderEngine engine, out ResourceLayout projViewLayout, out ResourceSet projViewSet)
        {
            var factory = engine.ResourceFactory;

            projViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ViewProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                    {
                        Name = "ViewProjectionBuffer_LayoutDescription"
                    }
                )
            );

            projViewSet = factory.CreateResourceSet(new ResourceSetDescription(
                projViewLayout,
                engine.GetSharedResource(CoreSharedResource.ViewProjectionBuffer)));
            projViewSet.Name = "ViewProj_ResourceSet";
        }

        private void RegisterGraphicsResource(uint slot, ResourceSet resourceSet)
        {
            m_graphicsResources[slot] = resourceSet;
        }
    }
}