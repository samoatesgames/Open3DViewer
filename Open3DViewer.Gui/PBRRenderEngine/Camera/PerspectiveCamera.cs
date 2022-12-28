using System.Numerics;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine.Camera
{
    public class PerspectiveCamera
    {
        private readonly DeviceBuffer m_projectionBuffer;
        private readonly DeviceBuffer m_viewBuffer;

        public PerspectiveCamera(PBRRenderEngine engine, ResourceFactory factory)
        {
            m_projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            m_viewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            engine.RegisterSharedResource(CoreSharedResource.ProjectionBuffer, m_projectionBuffer);
            engine.RegisterSharedResource(CoreSharedResource.ViewBuffer, m_viewBuffer);
        }
        
        public void GenerateCommands(CommandList commandList)
        {
            commandList.UpdateBuffer(m_projectionBuffer, 0, Matrix4x4.CreatePerspectiveFieldOfView(
                1.0f,
                (float)1280 / 720,
                0.5f,
                1000f));

            commandList.UpdateBuffer(m_viewBuffer, 0, Matrix4x4.CreateLookAt(Vector3.UnitZ * 5f, Vector3.Zero, Vector3.UnitY));
        }
    }
}