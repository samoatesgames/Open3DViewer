using System;
using System.Collections.Generic;
using OpenModelViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using OpenModelViewer.Gui.PBRRenderEngine.Shaders;
using OpenModelViewer.RenderViewControl;
using Veldrid;
using Veldrid.SPIRV;

namespace OpenModelViewer.Gui.PBRRenderEngine.Types
{
    public abstract class RenderMesh<TRenderEngine, TVertexLayout, TShader> : IDisposable
                                                      where TRenderEngine : IRenderEngine
                                                      where TVertexLayout : unmanaged, IVertexLayout 
                                                      where TShader : IRenderShader, new()
    {
        protected readonly TRenderEngine m_engine;
        
        protected Shader[] m_shaders;
        protected Pipeline m_pipeline;
        protected DeviceBuffer m_vertexBuffer;
        protected DeviceBuffer m_indexBuffer;
        protected uint m_indexCount;

        private readonly Dictionary<uint, ResourceSet> m_graphicsResources = new Dictionary<uint, ResourceSet>();

        // TODO: Shoud Index/vertex buffer should be in an initialize method?
        protected RenderMesh(TRenderEngine engine)
        {
            m_engine = engine;
        }

        public virtual void Initialize(TVertexLayout[] vertices, ushort[] indices)
        {
            var sizeInByes = vertices[0].GetSizeInBytes();
            m_indexCount = (uint)indices.Length;
            
            m_vertexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(vertices.Length * sizeInByes), BufferUsage.VertexBuffer));
            m_indexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
            
            m_engine.GraphicsDevice.UpdateBuffer(m_vertexBuffer, 0, vertices);
            m_engine.GraphicsDevice.UpdateBuffer(m_indexBuffer, 0, indices);
            
            var shader = new TShader(); // TODO: Is this more of a material?
            m_shaders = CreateShaders(m_engine.ResourceFactory, shader);
            m_pipeline = CreatePipeline(m_engine, shader);
        }

        public void Dispose()
        {
            m_vertexBuffer.Dispose();
            m_indexBuffer.Dispose();
            
            m_pipeline.Dispose();
            foreach (var shader in m_shaders)
            {
                shader.Dispose();
            }
        }

        protected void RegisterGraphicsResource(uint slot, ResourceSet resourceSet)
        {
            m_graphicsResources[slot] = resourceSet;
        }
        
        public virtual void Render(CommandList commandList)
        {
            commandList.SetVertexBuffer(0, m_vertexBuffer);
            commandList.SetIndexBuffer(m_indexBuffer, IndexFormat.UInt16);
            commandList.SetPipeline(m_pipeline);

            foreach (var entry in m_graphicsResources)
            {
                commandList.SetGraphicsResourceSet(entry.Key, entry.Value);
            }
            
            commandList.DrawIndexed(m_indexCount, 1, 0, 0, 0);
        }

        private Shader[] CreateShaders(ResourceFactory factory, TShader shader)
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

        protected abstract Pipeline CreatePipeline(TRenderEngine engine, TShader shader);
    }
}