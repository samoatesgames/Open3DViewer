using System;
using System.Numerics;
using Open3DViewer.PBRRenderer.Camera;
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

        public void Render(CommandList commandList, PerspectiveCamera camera)
        {
            if (!camera.CanSee(Scene.BoundingBox))
            {
                return;
            }

            Scene.Render(commandList, camera, Matrix4x4.Identity);
        }

        public BoundingBox GetBoundingBox()
        {
            return Scene.BoundingBox;
        }
    }
}