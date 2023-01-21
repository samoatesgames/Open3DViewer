using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Open3DViewer.PBRRenderer.Buffers.Vertex;
using Open3DViewer.PBRRenderer.Shaders;
using Open3DViewer.PBRRenderer.Types;
using SharpGLTF.Schema2;
using Veldrid;
using Vortice.Mathematics;

namespace Open3DViewer.PBRRenderer.GLTF
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

        private readonly ConcurrentDictionary<TextureSamplerIndex, TextureView> m_textureViews = new ConcurrentDictionary<TextureSamplerIndex, TextureView>();

        private readonly Material m_material;
        private MaterialInfo m_materialInfo;
        private readonly DeviceBuffer m_materialInfoBuffer;

        public BoundingBox BoundingBox { get; } 

        public GLTFMesh(PBRRenderEngine engine, Matrix4x4 localTransform, BoundingBox boundingBox, Material material)
        {
            m_engine = engine;
            m_material = material;

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
            if (textureView == null)
            {
                return;
            }

            textureView.Name = $"TextureView_{samplerIndex}";
            m_textureViews[samplerIndex] = textureView;
        }

        public void SetDiffuseTint(Vector4 tintColor)
        {
            m_materialInfo.Tint = tintColor;
        }

        public void SetMetallicRoughnessValues(float metallicFactor, float roughnessFactor)
        {
            m_materialInfo.MetallicRoughnessFactors = new Vector2(metallicFactor, roughnessFactor);
        }

        public void SetEmission(Vector3 emissiveTintColor, float emissiveStrength)
        {
            m_materialInfo.EmissiveFactors = new Vector4(emissiveTintColor, emissiveStrength);
        }

        public void SetOcclusion(float occlusionStrength)
        {
            m_materialInfo.OcclusionStrength = occlusionStrength;
        }

        public void Initialize(VertexLayoutFull[] vertices, uint[] indices)
        {
            var sizeInByes = vertices[0].GetSizeInBytes();
            m_indexCount = (uint)indices.Length;

            m_vertexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(vertices.Length * sizeInByes), BufferUsage.VertexBuffer));
            m_indexBuffer = m_engine.ResourceFactory.CreateBuffer(new BufferDescription((uint)(indices.Length * sizeof(uint)), BufferUsage.IndexBuffer));

            m_engine.GraphicsDevice.UpdateBuffer(m_vertexBuffer, 0, vertices);
            m_engine.GraphicsDevice.UpdateBuffer(m_indexBuffer, 0, indices);

#if DEBUG
            m_engine.ShaderResourceManager.OnShaderReloaded += OnShaderReloaded;
#endif
        }

        public void RecreatePipeline()
        {
            m_pipeline = CreatePipeline<ObjectShader>(m_engine);
        }

#if DEBUG
        private void OnShaderReloaded(IRenderShader shader)
        {
            if (!(shader is ObjectShader))
            {
                return;
            }
            RecreatePipeline();
        }
#endif

        public virtual void Render(CommandList commandList, Matrix4x4 worldTransform)
        {
            var transform = m_localTransform * worldTransform;
            commandList.UpdateBuffer(m_worldBuffer, 0, ref transform);
            
            commandList.UpdateBuffer(m_materialInfoBuffer, 0, ref m_materialInfo);

            commandList.SetVertexBuffer(0, m_vertexBuffer);
            commandList.SetIndexBuffer(m_indexBuffer, IndexFormat.UInt32);
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

            // Scene/Material info resources
            CreateConstantInfoResources(engine, out var constantInfoLayout, out var constantInfoSet);
            resourceLayouts.Add(constantInfoLayout);
            RegisterGraphicsResource(2, constantInfoSet);

            // Setup texture sampler resources
            SetupTextureSamplers(engine, out var textureLayouts);
            resourceLayouts.AddRange(textureLayouts);

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

        private void SetupTextureSamplers(PBRRenderEngine engine, out List<ResourceLayout> textureLayouts)
        {
            var factory = engine.ResourceFactory;
            textureLayouts = new List<ResourceLayout>();

            var samplerMask = 1;
            foreach (TextureSamplerIndex samplerType in Enum.GetValues(typeof(TextureSamplerIndex)))
            {
                var textureLayout = factory.CreateResourceLayout(
                    new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription($"{samplerType}Sampler", ResourceKind.Sampler,
                            ShaderStages.Fragment),
                        new ResourceLayoutElementDescription($"{samplerType}Texture", ResourceKind.TextureReadOnly,
                            ShaderStages.Fragment)
                    )
                );
                textureLayout.Name = $"{samplerType}_TextureLayout";
                textureLayouts.Add(textureLayout);

                if (!m_textureViews.TryGetValue(samplerType, out var textureView))
                {
                    textureView = m_engine.TextureResourceManager.GetFallbackTexture(samplerType);
                }
                else
                {
                    m_materialInfo.BoundTextureBitMask |= (uint)samplerMask;
                }

                samplerMask *= 2;

                var textureSet = engine.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
                    textureLayout,
                    engine.GraphicsDevice.LinearSampler,
                    textureView));
                textureSet.Name = $"{samplerType}_TextureSet";
                RegisterGraphicsResource((uint)samplerType + 3, textureSet);
            }
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

        private void CreateConstantInfoResources(PBRRenderEngine engine, out ResourceLayout materialInfoLayout, out ResourceSet materialInfoSet)
        {
            var factory = engine.ResourceFactory;

            materialInfoLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("SceneInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                    {
                        Name = "SceneInfoBuffer_LayoutDescription"
                    },
                    new ResourceLayoutElementDescription("MaterialInfo", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                    {
                        Name = "MaterialInfoBuffer_LayoutDescription"
                    }
                )
            );

            materialInfoSet = factory.CreateResourceSet(new ResourceSetDescription(
                materialInfoLayout,
                engine.SceneInfoBuffer,
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

        public List<Task> ProcessMaterial()
        {
            var textureResourceManager = m_engine.TextureResourceManager;

            var jobs = new List<Task>();

            foreach (var materialChannel in m_material.Channels)
            {
                jobs.Add(Task.Run(() =>
                {
                    TextureSamplerIndex samplerIndex;
                    if (materialChannel.Key == "BaseColor" || materialChannel.Key == "Diffuse")
                    {
                        samplerIndex = TextureSamplerIndex.Diffuse;

                        // See if we have a diffuse tint color
                        if (FindParameter<Vector4>(materialChannel.Parameters, "RGBA", out var rgba))
                        {
                            SetDiffuseTint(rgba);
                        }
                        else if (FindParameter<Vector3>(materialChannel.Parameters, "RGB", out var rgb))
                        {
                            SetDiffuseTint(new Vector4(rgb, 1.0f));
                        }
                    }
                    else if (materialChannel.Key == "Normal")
                    {
                        samplerIndex = TextureSamplerIndex.Normal;
                    }
                    else if (materialChannel.Key == "MetallicRoughness")
                    {
                        samplerIndex = TextureSamplerIndex.MetallicRoughness;

                        var metallicFactor = 1.0f;
                        var roughnessFactor = 1.0f;

                        // See if we have a fallback value
                        if (FindParameter<float>(materialChannel.Parameters, "MetallicFactor", out var metal))
                        {
                            metallicFactor = metal;
                        }

                        if (FindParameter<float>(materialChannel.Parameters, "RoughnessFactor", out var roughness))
                        {
                            roughnessFactor = roughness;
                        }

                        SetMetallicRoughnessValues(metallicFactor, roughnessFactor);
                    }
                    else if (materialChannel.Key == "Emissive")
                    {
                        samplerIndex = TextureSamplerIndex.Emissive;

                        var emissiveStrength = 0.0f;

                        if (FindParameter<float>(materialChannel.Parameters, "EmissiveStrength", out var strength))
                        {
                            emissiveStrength = strength;
                        }

                        var emissiveTintColor = Vector3.Zero;

                        // See if we have a emissive tint color
                        if (FindParameter<Vector4>(materialChannel.Parameters, "RGBA", out var rgba))
                        {
                            emissiveTintColor = new Vector3(rgba.X, rgba.Y, rgba.Z);
                        }
                        else if (FindParameter<Vector3>(materialChannel.Parameters, "RGB", out var rgb))
                        {
                            emissiveTintColor = rgb;
                        }

                        SetEmission(emissiveTintColor, emissiveStrength);
                    }
                    else if (materialChannel.Key == "Occlusion")
                    {
                        samplerIndex = TextureSamplerIndex.Occlusion;

                        if (FindParameter<float>(materialChannel.Parameters, "OcclusionStrength", out var occlusion))
                        {
                            SetOcclusion(occlusion);
                        }
                    }
                    else
                    {
                        // TODO: Log this unknown channel type so we can add support for it
                        return;
                    }

                    if (materialChannel.Texture != null)
                    {
                        SetTexture(samplerIndex, textureResourceManager.LoadTexture(materialChannel.Texture));
                    }
                }));
            }

            return jobs;
        }

        private bool FindParameter<TType>(IReadOnlyList<IMaterialParameter> parameters, string parameterName, out TType result)
        {
            var parameter = parameters.FirstOrDefault(x => x.Name == parameterName);
            if (parameter == null)
            {
                result = default;
                return false;
            }

            if (!(parameter.Value is TType value))
            {
                result = default;
                return false;
            }

            result = value;
            return true;
        }
    }
}