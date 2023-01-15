using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Open3DViewer.PBRRenderer.Buffers.Vertex;
using SharpGLTF.Schema2;
using Veldrid;
using Vortice.Mathematics;

namespace Open3DViewer.PBRRenderer.GLTF
{
    public class GLTFScene : IDisposable
    {
        public static async Task<GLTFScene> TryLoad(PBRRenderEngine engine, string gltfFilePath)
        {
            if (gltfFilePath == null)
            {
                return null;
            }
            
            if (!File.Exists(gltfFilePath))
            {
                return null;
            }

            try
            {
                var model = ModelRoot.Load(gltfFilePath);
                var scene = new GLTFScene(engine, model);
                await scene.Initialize();
                return scene;
            }
            catch
            {
                return null;
            }
        }

        private readonly PBRRenderEngine m_engine;
        private readonly List<GLTFMesh> m_meshes = new List<GLTFMesh>();

        public ModelRoot ModelRoot { get; }
        public BoundingBox BoundingBox { get; private set; }

        private GLTFScene(PBRRenderEngine engine, ModelRoot modelRoot)
        {
            m_engine = engine;
            ModelRoot = modelRoot;
        }

        private async Task Initialize()
        {
            // Pre load all textures
            var textureManager = m_engine.TextureResourceManager;
            var textureLoadJobs = new List<Task>();
            foreach (var texture in ModelRoot.LogicalTextures)
            {
                textureLoadJobs.Add(Task.Run(() =>
                {
                    textureManager.LoadTexture(texture);
                }));
            }

            // Load all meshes by recursing the scene graph.
            var meshLoadJobs = new List<Task<GLTFMesh>>();
            foreach (var scene in ModelRoot.LogicalScenes)
            {
                GenerateMeshLoadJobs(scene.VisualChildren, Matrix4x4.Identity, meshLoadJobs);
            }

            await Task.WhenAll(textureLoadJobs);
            var meshes = await Task.WhenAll(meshLoadJobs);

            var processMaterialJobs = new List<Task>();
            var sceneBounds = new BoundingBox();
            foreach (var mesh in meshes.Where(x => x != null))
            {
                // Apply the material parameters
                processMaterialJobs.AddRange(mesh.ProcessMaterial());

                // Update the scene bounds to include all the valid meshes bounds.
                sceneBounds = BoundingBox.CreateMerged(sceneBounds, mesh.BoundingBox);

                // Store our valid mesh, so we can render it.
                m_meshes.Add(mesh);
            }

            // Store the full scene bounds.
            BoundingBox = sceneBounds;

            await Task.WhenAll(processMaterialJobs);

            foreach (var mesh in m_meshes)
            {
                mesh.RecreatePipeline();
            }
        }

        private void GenerateMeshLoadJobs(IEnumerable<Node> nodes, Matrix4x4 parentTransform, List<Task<GLTFMesh>> meshLoadJobs)
        {
            foreach (var node in nodes)
            {
                if (node.Mesh != null)
                {
                    foreach (var primitive in node.Mesh.Primitives)
                    {
                        meshLoadJobs.Add(Task.Run(() =>
                        {
                            if (!TryCreateRenderMesh(m_engine, primitive, node.LocalMatrix * parentTransform, out var gltfMesh))
                            {
                                return null;
                            }
                            return gltfMesh;
                        }));
                    }
                }

                GenerateMeshLoadJobs(node.VisualChildren, node.LocalMatrix * parentTransform, meshLoadJobs);
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

        private bool TryCreateRenderMesh(PBRRenderEngine engine, MeshPrimitive primitive, Matrix4x4 transform, out GLTFMesh gltfMesh)
        {
            if (primitive.IndexAccessor == null)
            {
                // We must have indices to render something...
                // TODO: log error
                // TODO: Morph target gltfs don't have indices 
                gltfMesh = null;
                return false;
            }

            if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
            {
                // TODO: Support none triangle geometry
                // TODO: log error
                gltfMesh = null;
                return false;
            }

            var indices = new List<uint>();
            foreach (var i in primitive.IndexAccessor.AsIndicesArray())
            {
                indices.Add(i);
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
            gltfMesh = new GLTFMesh(engine, transform, boundingBox, primitive.Material);
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