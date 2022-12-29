﻿using Open3DViewer.Gui.PBRRenderEngine.GLTF;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using System.Numerics;
using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine.Camera
{
    public class PerspectiveCamera
    {
        private readonly DeviceBuffer m_projectionBuffer;
        private readonly DeviceBuffer m_viewBuffer;

        private float m_aspectRatio;
        private Matrix4x4 m_projectionMatrix;
        private GLTFEntity m_lookAtEntity;

        public PerspectiveCamera(PBRRenderEngine engine, ResourceFactory factory)
        {
            m_projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            m_viewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            engine.RegisterSharedResource(CoreSharedResource.ProjectionBuffer, m_projectionBuffer);
            engine.RegisterSharedResource(CoreSharedResource.ViewBuffer, m_viewBuffer);

            var frameBuffer = engine.Swapchain.Framebuffer;
            OnSwapchainResized(frameBuffer.Width, frameBuffer.Height);
        }

        public void OnSwapchainResized(uint width, uint height)
        {
            m_aspectRatio = (float)width / height;
            m_projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                1.0f,
                m_aspectRatio,
                0.005f,
                100f);
        }

        public void GenerateCommands(CommandList commandList)
        {
            commandList.UpdateBuffer(m_projectionBuffer, 0, m_projectionMatrix);

            if (m_lookAtEntity != null)
            {
                var lookAtBounds = m_lookAtEntity.GetBoundingBox();
                var boundsExtent = lookAtBounds.Extent.Length();

                var cameraPosition = lookAtBounds.Center
                                     + (Vector3.UnitX * (boundsExtent))
                                     + (Vector3.UnitY * (boundsExtent))
                                     + (Vector3.UnitZ * (boundsExtent * -2.5f));
                var cameraLookAt = lookAtBounds.Center;

                var lookAtMatrix = Matrix4x4.CreateLookAt(cameraPosition, cameraLookAt, Vector3.UnitY);
                commandList.UpdateBuffer(m_viewBuffer, 0, lookAtMatrix);
            }
            else
            {
                var lookAtMatrix = Matrix4x4.CreateLookAt(Vector3.UnitZ * -5f, Vector3.Zero, Vector3.UnitY);
                commandList.UpdateBuffer(m_viewBuffer, 0, lookAtMatrix);
            }
        }

        public void LookAt(GLTFEntity entity)
        {
            m_lookAtEntity = entity;
        }
    }
}