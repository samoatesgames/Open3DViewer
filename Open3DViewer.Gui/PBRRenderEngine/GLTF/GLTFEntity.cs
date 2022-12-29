using System;
using System.Numerics;
using Veldrid;
using Vortice.Mathematics;

namespace Open3DViewer.Gui.PBRRenderEngine.GLTF
{
    public class GLTFEntity : IDisposable
    {
        private readonly GLTFScene m_scene;
        private float m_ticks;
        
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
            m_ticks += 10.0f;

            var rotation = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, (-m_ticks / 1000f));
            var scale = Matrix4x4.CreateScale(1.0f);

            m_scene.Render(commandList, rotation * scale);
        }

        public BoundingBox GetBoundingBox()
        {
            return m_scene.BoundingBox;
        }
    }
}