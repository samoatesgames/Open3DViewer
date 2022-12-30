using Open3DViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Veldrid;
using Vortice.Mathematics;
using Texture = SharpGLTF.Schema2.Texture;

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

        public BoundingBox BoundingBox { get; }

        private GLTFScene(PBRRenderEngine engine, ModelRoot modelRoot)
        {
            var sceneBounds = new BoundingBox();

            foreach (var node in modelRoot.LogicalNodes)
            {
                if (node.Mesh != null)
                {
                    foreach (var primitive in node.Mesh.Primitives)
                    {
                        if (TryCreateRenderMesh(engine, primitive, node.LocalMatrix, out var gltfMesh))
                        {
                            sceneBounds = BoundingBox.CreateMerged(sceneBounds, gltfMesh.BoundingBox);
                            m_meshes.Add(gltfMesh);
                        }
                    }
                }
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
                foreach (var materialChannel in primitive.Material.Channels)
                {
                    if (materialChannel.Texture == null)
                    {
                        continue;
                    }
                    
                    SamplerIndex samplerIndex;
                    if (materialChannel.Key == "BaseColor" || materialChannel.Key == "Diffuse")
                    {
                        samplerIndex = SamplerIndex.Diffuse;
                    }
                    else if (materialChannel.Key == "Normal")
                    {
                        samplerIndex = SamplerIndex.Normal;
                    }
                    else if (materialChannel.Key == "MetallicRoughness")
                    {
                        samplerIndex = SamplerIndex.MetallicRoughness;
                    }
                    else if (materialChannel.Key == "Emissive")
                    {
                        samplerIndex = SamplerIndex.Emissive;
                    }
                    else if (materialChannel.Key == "Occlusion")
                    {
                        samplerIndex = SamplerIndex.Occlusion;
                    }
                    else
                    {
                        // TODO: Log this unknown channel type so we can add support for it
                        continue;
                    }

                    var loadedTexture = LoadTexture(engine, materialChannel.Texture);
                    gltfMesh.SetTexture(samplerIndex, loadedTexture);
                }
            }

            gltfMesh.Initialize(vertices.ToArray(), indices.ToArray());
            return true;
        }

        public static Veldrid.Texture LoadTexture(PBRRenderEngine engine, Texture inputTexture)
        {
            using (var stream = new MemoryStream(inputTexture.PrimaryImage.Content.Content.ToArray()))
            {
                return LoadTexture(engine, stream);
            }
        }

        public static Veldrid.Texture LoadTexture(PBRRenderEngine engine, Stream stream)
        {
            var imageSharpTexture = new Veldrid.ImageSharp.ImageSharpTexture(stream, false);
            var deviceTexture = imageSharpTexture.CreateDeviceTexture(engine.GraphicsDevice, engine.ResourceFactory);
            return deviceTexture;
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