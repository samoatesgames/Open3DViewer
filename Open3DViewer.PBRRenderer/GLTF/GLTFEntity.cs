using System;
using System.Numerics;
using Veldrid;
using Vortice.Mathematics;

namespace Open3DViewer.PBRRenderer.GLTF
{
    public class GLTFEntity : IDisposable
    {
        public GLTFScene Scene { get; }

        public GLTFEntity(GLTFScene scene)
        {
            Scene = scene;
        }

        public void Dispose()
        {
            Scene.Dispose();
        }

        public void Render(CommandList commandList)
        {
            Scene.Render(commandList, Matrix4x4.Identity);
        }

        public BoundingBox GetBoundingBox()
        {
            return Scene.BoundingBox;
        }
    }
}