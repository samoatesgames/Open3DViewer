﻿using Open3DViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using SharpGLTF.Schema2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Veldrid;
using Vortice.Mathematics;

namespace Open3DViewer.Gui.PBRRenderEngine.GLTF
{
    public class GLTFScene : IDisposable
    {
        public static bool TryLoad(PBRRenderEngine engine, string gltfFilePath, out GLTFScene scene)
        {
            if (gltfFilePath == null)
            {
                scene = null;
                return false;
            }
            
            if (!File.Exists(gltfFilePath))
            {
                scene = null;
                return false;
            }

            try
            {
                var model = ModelRoot.Load(gltfFilePath);
                scene = new GLTFScene(engine, model);
                return true;
            }
            catch
            {
                scene = null;
                return false;
            }
        }
        
        private readonly List<GLTFMesh> m_meshes = new List<GLTFMesh>();

        public ModelRoot ModelRoot { get; }
        public BoundingBox BoundingBox { get; }

        private GLTFScene(PBRRenderEngine engine, ModelRoot modelRoot)
        {
            ModelRoot = modelRoot;

            var primitiveMap = new Dictionary<MeshPrimitive, Node>();
            foreach (var node in modelRoot.LogicalNodes)
            {
                if (node.Mesh == null)
                {
                    continue;
                }

                foreach (var primitive in node.Mesh.Primitives)
                {
                    primitiveMap[primitive] = node;
                }
            }

            var createdMeshes = new ConcurrentBag<GLTFMesh>();
            Parallel.ForEach(primitiveMap, primitiveEntry =>
            {
                var primitive = primitiveEntry.Key;
                var node = primitiveEntry.Value;

                if (TryCreateRenderMesh(engine, primitive, node.LocalMatrix, out var gltfMesh))
                {
                    createdMeshes.Add(gltfMesh);
                }
            });

            var sceneBounds = new BoundingBox();
            foreach (var mesh in createdMeshes)
            {
                sceneBounds = BoundingBox.CreateMerged(sceneBounds, mesh.BoundingBox);
                m_meshes.Add(mesh);
            }
            BoundingBox = sceneBounds;
        }
        
        public void Dispose()
        {
            foreach (var mesh in m_meshes)
            {
                mesh.Dispose();
            }
            m_meshes.Clear();
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

        private bool TryCreateRenderMesh(PBRRenderEngine engine, MeshPrimitive primitive, Matrix4x4 transform, out GLTFMesh gltfMesh)
        {
            if (primitive.IndexAccessor == null)
            {
                // We must have indices to render something...
                // TODO: log error
                gltfMesh = null;
                return false;
            }

            var indices = new List<ushort>();
            foreach (var i in primitive.IndexAccessor.AsIndicesArray())
            {
                indices.Add((ushort)i);
            }

            IList<Vector3> positionStream = null;
            IList<Vector3> normalStream = null;
            IList<Vector4> tangentStream = null;
            IList<Vector2> uv0Stream = null;
            
            foreach (var vertexAccessorDesc in primitive.VertexAccessors)
            {
                var name = vertexAccessorDesc.Key;
                var accessor = vertexAccessorDesc.Value;

                switch (name)
                {
                    case "POSITION":
                        positionStream = accessor.AsVector3Array();
                        break;
                    case "NORMAL":
                        normalStream = accessor.AsVector3Array();
                        break;
                    case "TANGENT":
                        tangentStream = accessor.AsVector4Array();
                        break;
                    case "TEXCOORD_0":
                        uv0Stream = accessor.AsVector2Array();
                        break;
                }
            }

            if (positionStream == null)
            {
                // We must have a position stream to render something...
                // TODO: log error
                gltfMesh = null;
                return false;
            }
            
            var vertices = new List<VertexLayoutFull>();
            
            for (var vertexIndex = 0; vertexIndex < positionStream.Count; ++vertexIndex)
            {
                var vertex = new VertexLayoutFull();

                var pos = positionStream[vertexIndex];
                vertex.PositionX = pos.X;
                vertex.PositionY = pos.Y;
                vertex.PositionZ = pos.Z;

                if (normalStream != null)
                {
                    var normal = normalStream[vertexIndex];
                    vertex.NormalX = normal.X;
                    vertex.NormalY = normal.Y;
                    vertex.NormalZ = normal.Z;

                    if (tangentStream != null)
                    {
                        var tangent = tangentStream[vertexIndex];
                        vertex.TangentX = tangent.X;
                        vertex.TangentY = tangent.Y;
                        vertex.TangentZ = tangent.Z;

                        var biTangent = Vector3.Cross(normal, new Vector3(tangent.X, tangent.Y, tangent.Z)) * tangent.W;
                        vertex.BiTangentX = biTangent.X;
                        vertex.BiTangentY = biTangent.Y;
                        vertex.BiTangentZ = biTangent.Z;
                    }
                    else
                    {
                        // Calculate our tangent/bitangent
                        var t1 = Vector3.Cross(normal, Vector3.UnitZ);
                        var t2 = Vector3.Cross(normal, Vector3.UnitY);
                        var tangent = t1.LengthSquared() > t2.LengthSquared() ? t1 : t2;
                        var biTangent = Vector3.Cross(normal, tangent);

                        vertex.TangentX = tangent.X;
                        vertex.TangentY = tangent.Y;
                        vertex.TangentZ = tangent.Z;

                        vertex.BiTangentX = biTangent.X;
                        vertex.BiTangentY = biTangent.Y;
                        vertex.BiTangentZ = biTangent.Z;
                    }
                }

                if (uv0Stream != null)
                {
                    var uv0 = uv0Stream[vertexIndex];
                    vertex.TexU0 = uv0.X;
                    vertex.TexV0 = uv0.Y;
                }
                
                vertices.Add(vertex);
            }

            var transformedVertexPositions = positionStream.Select(x => Vector3.Transform(x, transform)).ToArray();
            var boundingBox = BoundingBox.CreateFromPoints(transformedVertexPositions);
            gltfMesh = new GLTFMesh(engine, transform, boundingBox);

            if (primitive.Material != null)
            {
                var loadedTextures = new ConcurrentDictionary<TextureSamplerIndex, TextureView>();
                var diffuseTintColor = Vector4.One;

                var metallicFactor = 0.0f;
                var roughnessFactor = 0.0f;

                var emissiveStrength = 1.0f;
                var emissiveTintColor = Vector3.Zero;

                var occlusionStrength = 1.0f;

                Parallel.ForEach(primitive.Material.Channels, materialChannel =>
                {
                    TextureSamplerIndex samplerIndex;
                    if (materialChannel.Key == "BaseColor" || materialChannel.Key == "Diffuse")
                    {
                        samplerIndex = TextureSamplerIndex.Diffuse;

                        // See if we have a diffuse tint color
                        if (FindParameter<Vector4>(materialChannel.Parameters, "RGBA", out var rgba))
                        {
                            diffuseTintColor = rgba;
                        }
                        else if (FindParameter<Vector3>(materialChannel.Parameters, "RGB", out var rgb))
                        {
                            diffuseTintColor = new Vector4(rgb, 1.0f);
                        }
                    }
                    else if (materialChannel.Key == "Normal")
                    {
                        samplerIndex = TextureSamplerIndex.Normal;
                    }
                    else if (materialChannel.Key == "MetallicRoughness")
                    {
                        samplerIndex = TextureSamplerIndex.MetallicRoughness;

                        // See if we have a fallback value
                        if (FindParameter<float>(materialChannel.Parameters, "MetallicFactor", out var metal))
                        {
                            metallicFactor = metal;
                        }
                        
                        if (FindParameter<float>(materialChannel.Parameters, "RoughnessFactor", out var roughness))
                        {
                            roughnessFactor = roughness;
                        }
                    }
                    else if (materialChannel.Key == "Emissive")
                    {
                        samplerIndex = TextureSamplerIndex.Emissive;

                        if (FindParameter<float>(materialChannel.Parameters, "EmissiveStrength", out var strength))
                        {
                            emissiveStrength = strength;
                        }

                        // See if we have a emissive tint color
                        if (FindParameter<Vector4>(materialChannel.Parameters, "RGBA", out var rgba))
                        {
                            emissiveTintColor = new Vector3(rgba.X, rgba.Y, rgba.Z);
                        }
                        else if (FindParameter<Vector3>(materialChannel.Parameters, "RGB", out var rgb))
                        {
                            emissiveTintColor = rgb;
                        }
                    }
                    else if (materialChannel.Key == "Occlusion")
                    {
                        samplerIndex = TextureSamplerIndex.Occlusion;

                        if (FindParameter<float>(materialChannel.Parameters, "OcclusionStrength", out var occlusion))
                        {
                            occlusionStrength = occlusion;
                        }
                    }
                    else
                    {
                        // TODO: Log this unknown channel type so we can add support for it
                        return;
                    }
                    
                    if (materialChannel.Texture != null)
                    {
                        var loadedTexture = engine.TextureResourceManager.LoadTexture(materialChannel.Texture);
                        loadedTextures[samplerIndex] = loadedTexture;
                    }
                });

                gltfMesh.SetDiffuseTint(diffuseTintColor);
                gltfMesh.SetMetallicRoughnessValues(metallicFactor, roughnessFactor);
                gltfMesh.SetEmission(emissiveTintColor, emissiveStrength);
                gltfMesh.SetOcclusion(occlusionStrength);

                foreach (var entry in loadedTextures)
                {
                    gltfMesh.SetTexture(entry.Key, entry.Value);
                }
            }

            gltfMesh.Initialize(vertices.ToArray(), indices.ToArray());
            return true;
        }
        
        public void Render(CommandList commandList, Matrix4x4 worldTransform)
        {
            foreach (var mesh in m_meshes)
            {
                mesh.Render(commandList, worldTransform);
            }
        }
    }
}