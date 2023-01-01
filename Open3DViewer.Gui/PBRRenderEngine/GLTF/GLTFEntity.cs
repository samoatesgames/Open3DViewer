using System;
using System.Numerics;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using Veldrid;
using Vortice.Mathematics;

namespace Open3DViewer.Gui.PBRRenderEngine.GLTF
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

        public void SetShadingMode(ShadingModes shadingMode)
        {
            Scene.SetShadingMode(shadingMode);
        }

        public BoundingBox GetBoundingBox()
        {
            return Scene.BoundingBox;
        }
    }
}