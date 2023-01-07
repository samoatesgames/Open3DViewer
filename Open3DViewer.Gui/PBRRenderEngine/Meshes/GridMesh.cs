using System;
using System.Collections.Generic;
using System.Numerics;
using Open3DViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using Open3DViewer.Gui.PBRRenderEngine.Shaders;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine.Meshes
{
    public class GridMesh : IDisposable
    {
        private readonly PBRRenderEngine m_engine;
        private readonly DeviceBuffer m_vertexBuffer;
        private readonly uint m_vertexCount;
        private readonly Dictionary<uint, ResourceSet> m_graphicsResources = new Dictionary<uint, ResourceSet>();

        private Pipeline m_pipeline;

        public GridMesh(PBRRenderEngine engine, float gridSize, float yPosition)
        {
            m_engine = engine;
            
            CreateVerticesAndIndices(gridSize, yPosition, out var vertices);

            m_vertexCount = (uint)vertices.Length;
            var sizeInByes = vertices[0].GetSizeInBytes();

            m_vertexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(vertices.Length * sizeInByes), BufferUsage.VertexBuffer));
            m_engine.GraphicsDevice.UpdateBuffer(m_vertexBuffer, 0, vertices);

            m_pipeline = CreatePipeline<GridShader>(m_engine);

#if DEBUG
            m_engine.ShaderResourceManager.OnShaderReloaded += OnShaderReloaded;
#endif
        }

#if DEBUG
        private void OnShaderReloaded(IRenderShader shader)
        {
            if (!(shader is GridShader))
            {
                return;
            }
            m_pipeline = CreatePipeline<GridShader>(m_engine);
        }
#endif

        public void Dispose()
        {
            m_pipeline?.Dispose();
            m_vertexBuffer?.Dispose();
        }

        private void CreateVerticesAndIndices(float gridSize, float yPosition, out GridVertexLayout[] vertices)
        {
            var minorGridCount = 20;
            var size = gridSize * 2.0f;
            var step = (size / minorGridCount) * 2.0f;

            var white = Vector3.One * 0.9f;

            var verts = new List<GridVertexLayout>();

            var xPos = -size;
            for (var xIndex = 0; xIndex <= minorGridCount; ++xIndex)
            {
                var color = xIndex == minorGridCount / 2 ? new Vector3(0, 0, 0.9f) : white;
                verts.Add(new GridVertexLayout(xPos, yPosition, size, color));
                verts.Add(new GridVertexLayout(xPos, yPosition, -size, color));
                xPos += step;
            }

            var zPos = -size;
            for (var zIndex = 0; zIndex <= minorGridCount; ++zIndex)
            {
                var color = zIndex == minorGridCount / 2 ? new Vector3(0.9f, 0, 0) : white;
                verts.Add(new GridVertexLayout(size, yPosition, zPos, color));
                verts.Add(new GridVertexLayout(-size, yPosition, zPos, color));
                zPos += step;
            }

            vertices = verts.ToArray();
        }

        private Pipeline CreatePipeline<TShader>(PBRRenderEngine engine) where TShader : IRenderShader
        {
            var factory = engine.ResourceFactory;

            var resourceLayouts = new List<ResourceLayout>();

            // Camera Project/View resources
            CreateProjectionViewResources(engine, out var projViewLayout, out var projViewSet);
            resourceLayouts.Add(projViewLayout);
            RegisterGraphicsResource(0, projViewSet);

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
                PrimitiveTopology = PrimitiveTopology.LineList,
                ResourceLayouts = resourceLayouts.ToArray(),
                ShaderSet = m_engine.ShaderResourceManager.GetShaderSet<TShader>(),
                Outputs = engine.Swapchain.Framebuffer.OutputDescription
            };

            return factory.CreateGraphicsPipeline(pipelineDescription);
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

        public void Render(CommandList commandList)
        {
            commandList.SetVertexBuffer(0, m_vertexBuffer);
            commandList.SetPipeline(m_pipeline);

            foreach (var entry in m_graphicsResources)
            {
                commandList.SetGraphicsResourceSet(entry.Key, entry.Value);
            }

            commandList.Draw(m_vertexCount);
        }
    }
}
