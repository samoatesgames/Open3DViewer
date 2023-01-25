using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Open3DViewer.PBRRenderer.Buffers.Vertex;
using Open3DViewer.PBRRenderer.Camera;
using Open3DViewer.PBRRenderer.Types;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
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
                var model = ModelRoot.Load(gltfFilePath, new ReadSettings
                {
                    Validation = ValidationMode.TryFix
                });
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
            IList<Vector4> color0Stream = null;
            
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
                    case "COLOR_0":
                        color0Stream = accessor.AsColorArray();
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

            if (normalStream == null)
            {
                // TODO: Generate flat normals
                gltfMesh = null;
                return false;
            }

            if (tangentStream == null)
            {
                if (uv0Stream != null)
                {
                    GenerateTangents(indices, positionStream, normalStream, uv0Stream, out tangentStream);
                }
                else
                {
                    tangentStream = new List<Vector4>();
                    foreach (var normal in normalStream)
                    {
                        var t1 = Vector3.Cross(normal, Vector3.UnitZ);
                        var t2 = Vector3.Cross(normal, Vector3.UnitY);
                        var tangent = t1.LengthSquared() > t2.LengthSquared() ? t1 : t2;
                        var biTangent = Vector3.Cross(normal, tangent);

                        var handedness = Vector3.Dot(Vector3.Cross(tangent, normal), biTangent) < 0 ? -1.0f : 1.0f;
                        tangentStream.Add(new Vector4(tangent.X, tangent.Y, tangent.Z, handedness));
                    }
                }
            }

            var vertices = new List<VertexLayoutFull>();
            
            for (var vertexIndex = 0; vertexIndex < positionStream.Count; ++vertexIndex)
            {
                var vertex = new VertexLayoutFull();

                var pos = positionStream[vertexIndex];
                vertex.PositionX = pos.X;
                vertex.PositionY = pos.Y;
                vertex.PositionZ = pos.Z;

                var normal = normalStream[vertexIndex];
                vertex.NormalX = normal.X;
                vertex.NormalY = normal.Y;
                vertex.NormalZ = normal.Z;

                var tangent = tangentStream[vertexIndex];
                vertex.TangentX = tangent.X;
                vertex.TangentY = tangent.Y;
                vertex.TangentZ = tangent.Z;

                var biTangent = Vector3.Cross(normal, new Vector3(tangent.X, tangent.Y, tangent.Z)) * tangent.W;
                vertex.BiTangentX = biTangent.X;
                vertex.BiTangentY = biTangent.Y;
                vertex.BiTangentZ = biTangent.Z;

                if (uv0Stream != null)
                {
                    var uv0 = uv0Stream[vertexIndex];
                    vertex.TexU0 = uv0.X;
                    vertex.TexV0 = uv0.Y;
                }

                var color = Vector4.One;
                if (color0Stream != null)
                {
                    color = color0Stream[vertexIndex];
                }
                vertex.ColorR = color.X;
                vertex.ColorG = color.Y;
                vertex.ColorB = color.Z;
                vertex.ColorA = color.W;

                vertices.Add(vertex);
            }

            var transformedVertexPositions = positionStream.Select(x => Vector3.Transform(x, transform)).ToArray();
            var boundingBox = BoundingBox.CreateFromPoints(transformedVertexPositions);
            gltfMesh = new GLTFMesh(engine, transform, boundingBox, primitive.Material);
            gltfMesh.Initialize(vertices.ToArray(), indices.ToArray());
            return true;
        }

        private void GenerateTangents(List<uint> indices, IList<Vector3> positionStream, IList<Vector3> normalStream, IList<Vector2> uv0Stream, out IList<Vector4> tangentStream)
        {
            void addTangent(Dictionary<System.ValueTuple<Vector3, Vector3, Vector2>, (Vector3, Vector3)> dict, System.ValueTuple<Vector3, Vector3, Vector2> key, (Vector3 tu, Vector3 tv) alpha)
            {
                dict.TryGetValue(key, out (Vector3 tu, Vector3 tv) beta);
                dict[key] = (alpha.tu + beta.tu, alpha.tv + beta.tv);
            }

            bool isFinite(float value)
            {
                return !(float.IsNaN(value) || float.IsInfinity(value));
            }

            bool isFiniteVec3(Vector3 v)
            {
                return isFinite(v.X) && isFinite(v.Y) && isFinite(v.Z);
            }

            var tangentsMap = new Dictionary<System.ValueTuple<Vector3, Vector3, Vector2>, (Vector3 u, Vector3 v)>();

            // calculate
            for (var i = 0; i < indices.Count; i += 3)
            {
                var i1 = (int)indices[i + 0];
                var i2 = (int)indices[i + 1];
                var i3 = (int)indices[i + 2];

                var p1 = positionStream[i1];
                var p2 = positionStream[i2];
                var p3 = positionStream[i3];

                // check for degenerated triangle
                if (p1 == p2 || p1 == p3 || p2 == p3) continue;

                var uv1 = uv0Stream[i1];
                var uv2 = uv0Stream[i2];
                var uv3 = uv0Stream[i3];

                // check for degenerated triangle
                if (uv1 == uv2 || uv1 == uv3 || uv2 == uv3) continue;

                var n1 = normalStream[i1];
                var n2 = normalStream[i2];
                var n3 = normalStream[i3];

                // calculate tangents
                var svec = p2 - p1;
                var tvec = p3 - p1;

                var stex = uv2 - uv1;
                var ttex = uv3 - uv1;

                float sx = stex.X;
                float tx = ttex.X;
                float sy = stex.Y;
                float ty = ttex.Y;

                var r = 1.0F / ((sx * ty) - (tx * sy));

                if (!isFinite(r)) continue;

                var sdir = new Vector3((ty * svec.X) - (sy * tvec.X), (ty * svec.Y) - (sy * tvec.Y), (ty * svec.Z) - (sy * tvec.Z)) * r;
                var tdir = new Vector3((sx * tvec.X) - (tx * svec.X), (sx * tvec.Y) - (tx * svec.Y), (sx * tvec.Z) - (tx * svec.Z)) * r;

                if (!isFiniteVec3(sdir)) continue;
                if (!isFiniteVec3(tdir)) continue;

                // accumulate tangents
                addTangent(tangentsMap, (p1, n1, uv1), (sdir, tdir));
                addTangent(tangentsMap, (p2, n2, uv2), (sdir, tdir));
                addTangent(tangentsMap, (p3, n3, uv3), (sdir, tdir));
            }

            // normalize
            foreach (var key in tangentsMap.Keys.ToList())
            {
                var val = tangentsMap[key];

                // Gram-Schmidt orthogonalize
                val.u = Vector3.Normalize(val.u - (key.Item2 * Vector3.Dot(key.Item2, val.u)));
                val.v = Vector3.Normalize(val.v - (key.Item2 * Vector3.Dot(key.Item2, val.v)));

                tangentsMap[key] = val;
            }

            // apply
            tangentStream = new List<Vector4>(new Vector4[positionStream.Count]);
            for (var i = 0; i < positionStream.Count; ++i)
            {
                var p = positionStream[i];
                var n = normalStream[i];
                var t = uv0Stream[i];

                if (tangentsMap.TryGetValue((p, n, t), out (Vector3 u, Vector3 v) tangents))
                {
                    var handedness = Vector3.Dot(Vector3.Cross(tangents.u, n), tangents.v) < 0 ? -1.0f : 1.0f;
                    tangentStream[i] = new Vector4(tangents.u, handedness);
                }
                else
                {
                    tangentStream[i] = new Vector4(1, 0, 0, 1);
                }
            }
        }

        public void Render(CommandList commandList, PerspectiveCamera camera, RenderPass renderPass, Matrix4x4 worldTransform)
        {
            foreach (var mesh in m_meshes)
            {
                if (!camera.CanSee(mesh.BoundingBox))
                {
                    continue;
                }
                mesh.Render(commandList, renderPass, worldTransform);
            }
        }
    }
}