using System;
using System.Numerics;
using Veldrid;
using Vortice.Mathematics;

namespace Open3DViewer.Gui.PBRRenderEngine.GLTF
{
    public class GLTFEntity : IDisposable
    {
        private readonly GLTFScene m_scene;

        public GLTFEntity(GLTFScene scene)
        {
            m_scene = scene;
        }

        public void Dispose()
        {
            m_scene.Dispose();
        }

        public void Render(CommandList commandList)
        {
            m_scene.Render(commandList, Matrix4x4.Identity);
        }

        public BoundingBox GetBoundingBox()
        {
            return m_scene.BoundingBox;
        }
    }
}