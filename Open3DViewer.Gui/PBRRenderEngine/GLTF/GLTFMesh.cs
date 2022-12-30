using Open3DViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using Open3DViewer.Gui.PBRRenderEngine.Shaders;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Vortice.Mathematics;

namespace Open3DViewer.Gui.PBRRenderEngine.GLTF
{
    public enum SamplerIndex : uint
    {
        Diffuse = 0,
        Normal = 1,
        MetallicRoughness = 2,
        Emissive = 4,
        Occlusion = 8
    }

    public class GLTFMesh : IDisposable
    {
        private readonly PBRRenderEngine m_engine;
        private readonly Dictionary<uint, ResourceSet> m_graphicsResources = new Dictionary<uint, ResourceSet>();
        
        private readonly DeviceBuffer m_worldBuffer;
        private readonly Matrix4x4 m_localTransform;

        private Shader[] m_shaders;
        private Pipeline m_pipeline;
        
        private DeviceBuffer m_vertexBuffer;
        private DeviceBuffer m_indexBuffer;
        private uint m_indexCount;

        private readonly Dictionary<SamplerIndex, Texture> m_textures = new Dictionary<SamplerIndex, Texture>();
        private readonly Dictionary<SamplerIndex, TextureView> m_textureViews = new Dictionary<SamplerIndex, TextureView>();
        private readonly Dictionary<SamplerIndex, TextureView> m_fallbackTextureViews = new Dictionary<SamplerIndex, TextureView>();

#if DEBUG
        private readonly List<FileSystemWatcher> m_shaderWatchers = new List<FileSystemWatcher>();
#endif

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

            foreach (var textureView in m_textureViews.Values)
            {
                textureView.Dispose();
            }
            m_textureViews.Clear();

            foreach (var textureView in m_fallbackTextureViews.Values)
            {
                textureView.Dispose();
            }
            m_fallbackTextureViews.Clear();

            m_pipeline.Dispose();
            foreach (var shader in m_shaders)
            {
                shader.Dispose();
            }

#if DEBUG
            foreach (var watcher in m_shaderWatchers)
            {
                watcher.Dispose();
            }
            m_shaderWatchers.Clear();
#endif
        }

        public void SetTexture(SamplerIndex samplerIndex, Texture texture)
        {
            texture.Name = samplerIndex.ToString();
            m_textures[samplerIndex] = texture;
        }

        public void Initialize(VertexLayoutFull[] vertices, ushort[] indices)
        {
            foreach (var textureEntry in m_textures)
            {
                var textureView = m_engine.ResourceFactory.CreateTextureView(textureEntry.Value);
                textureView.Name = $"TextureView_{textureEntry.Value.Name}";
                m_textureViews[textureEntry.Key] = textureView;
            }

            // TODO: This should be done once for the entire application, not per mesh
            using (var stream = new FileStream("Assets/Fallback Assets/DefaultDiffuseMap.png", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var defaultDiffuseTextureView = m_engine.ResourceFactory.CreateTextureView(GLTFScene.LoadTexture(m_engine, stream));
                defaultDiffuseTextureView.Name = $"DefaultTextureView_{SamplerIndex.Diffuse}";
                m_fallbackTextureViews[SamplerIndex.Diffuse] = defaultDiffuseTextureView;
            }
            // TODO: End of todo

            var sizeInByes = vertices[0].GetSizeInBytes();
            m_indexCount = (uint)indices.Length;

            m_vertexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(vertices.Length * sizeInByes), BufferUsage.VertexBuffer));
            m_indexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));

            m_engine.GraphicsDevice.UpdateBuffer(m_vertexBuffer, 0, vertices);
            m_engine.GraphicsDevice.UpdateBuffer(m_indexBuffer, 0, indices);

            var shader = new ObjectShader();
            CreateShaders(m_engine.ResourceFactory, shader);

#if DEBUG
            WatchForShaderChanges(m_engine, shader);
#endif

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

        private void CreateShaders(ResourceFactory factory, ObjectShader shader)
        {
            var vertexShaderDesc = new ShaderDescription(
                ShaderStages.Vertex,
                ShaderFileToBytes(shader.GetVertexShaderPath()),
                "main");
            var fragmentShaderDesc = new ShaderDescription(
                ShaderStages.Fragment,
                ShaderFileToBytes(shader.GetPixelShaderPath()),
                "main");

            m_shaders = factory.CreateFromSpirv(vertexShaderDesc, fragmentShaderDesc);
        }

        private byte[] ShaderFileToBytes(string filePath)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    return Encoding.UTF8.GetBytes(reader.ReadToEnd());
                }
            }
        }

#if DEBUG
        private void WatchForShaderChanges(PBRRenderEngine engine, ObjectShader shader)
        {
            foreach (var shaderFile in new[]
                     {
                         shader.GetVertexShaderPath(),
                         shader.GetPixelShaderPath()
                     })
            {
                var fileDirectory = Path.GetDirectoryName(shaderFile);
                var file = Path.GetFileName(shaderFile);
                var watcher = new FileSystemWatcher(fileDirectory, file)
                {
                    NotifyFilter = NotifyFilters.Attributes
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.DirectoryName
                                   | NotifyFilters.FileName
                                   | NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.Security
                                   | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                watcher.Changed += (sender, args) =>
                {
                    CreateShaders(engine.ResourceFactory, shader);
                    m_pipeline = CreatePipeline(engine, shader);
                };
                m_shaderWatchers.Add(watcher);
            }
        }
#endif

        private Pipeline CreatePipeline(PBRRenderEngine engine, ObjectShader shader)
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
            foreach (SamplerIndex samplerType in Enum.GetValues(typeof(SamplerIndex)))
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
                    textureView = m_fallbackTextureViews[SamplerIndex.Diffuse];
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
                ShaderSet = new ShaderSetDescription
                (
                    vertexLayouts: new [] { shader.GetVertexLayout() },
                    shaders: m_shaders
                ),
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