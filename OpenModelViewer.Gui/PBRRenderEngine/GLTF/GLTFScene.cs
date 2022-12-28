using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using OpenModelViewer.Gui.PBRRenderEngine.Buffers.Vertex;
using OpenModelViewer.RenderViewControl;
using SharpGLTF.Schema2;
using Veldrid;
using Texture = SharpGLTF.Schema2.Texture;

namespace OpenModelViewer.Gui.PBRRenderEngine.GLTF
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
            
            var model = ModelRoot.Load(gltfFilePath);
            scene = new GLTFScene(engine, model);
            return true;
        }
        
        private readonly List<GLTFMesh> m_meshes = new List<GLTFMesh>();
        
        private GLTFScene(PBRRenderEngine engine, ModelRoot modelRoot)
        {
            foreach (var mesh in modelRoot.LogicalMeshes)
            {
                foreach (var primitive in mesh.Primitives)
                {
                    m_meshes.Add(CreateRenderMesh(engine, primitive));
                }
            }
        }
        
        public void Dispose()
        {
            foreach (var mesh in m_meshes)
            {
                mesh.Dispose();
            }
            m_meshes.Clear();
        }

        private GLTFMesh CreateRenderMesh(PBRRenderEngine engine, MeshPrimitive primitive)
        {
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
                var emptyMesh = new GLTFMesh(engine);
                emptyMesh.Initialize(Array.Empty<VertexLayoutFull>(), indices.ToArray());
                return emptyMesh;
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

            var mesh = new GLTFMesh(engine);

            var diffuseTexture = primitive.Material?.GetDiffuseTexture();
            if (diffuseTexture != null)
            {
                var loadedTexture = LoadTexture(engine, diffuseTexture);
                mesh.SetTexture(0, loadedTexture);
            }
            
            mesh.Initialize(vertices.ToArray(), indices.ToArray());
            return mesh;
        }

        private Veldrid.Texture LoadTexture(PBRRenderEngine engine, Texture diffuseTexture)
        {
            using (var stream = new MemoryStream(diffuseTexture.PrimaryImage.Content.Content.ToArray()))
            {
                var a = new Veldrid.ImageSharp.ImageSharpTexture(stream, false);
                return a.CreateDeviceTexture(engine.GraphicsDevice, engine.ResourceFactory);
            }
        }

        public void SetWorldBuffer(IRenderEngine engine, DeviceBuffer worldBuffer)
        {
            foreach (var mesh in m_meshes)
            {
                mesh.SetWorldBuffer(engine, worldBuffer);
            }
        }

        public void Render(CommandList commandList)
        {
            foreach (var mesh in m_meshes)
            {
                mesh.Render(commandList);
            }
        }
    }
}