using System;
using System.Numerics;
using Open3DViewer.RenderViewControl;
using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine.GLTF
{
    public class GLTFEntity<TRenderEngine> : IDisposable 
        where TRenderEngine : IRenderEngine
    {
        private readonly GLTFScene m_scene;
        private readonly DeviceBuffer m_worldBuffer;
        private float m_ticks;
        
        public GLTFEntity(TRenderEngine engine, GLTFScene scene)
        {
            m_scene = scene;
            m_worldBuffer = engine.ResourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            m_scene.SetWorldBuffer(engine, m_worldBuffer);
        }

        public void Dispose()
        {
            m_scene.Dispose();
        }

        public void Render(CommandList commandList)
        {
            var rotationX = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, 90.0f * ((float)Math.PI / 180.0f));
            var rotationY = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, (m_ticks / 1000f));
            var rotation = rotationX * rotationY;
            commandList.UpdateBuffer(m_worldBuffer, 0, ref rotation);
            m_ticks += 10.0f;
            
            m_scene.Render(commandList);
        }
    }
}