using Open3DViewer.Gui.PBRRenderEngine.GLTF;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine.Camera
{
    public class PerspectiveCamera
    {
        private enum MouseMoveMode
        {
            None,
            Orbit,
            Pan
        }

        private readonly DeviceBuffer m_projectionBuffer;
        private readonly DeviceBuffer m_viewBuffer;

        private float m_aspectRatio;
        private Matrix4x4 m_projectionMatrix;
        private GLTFEntity m_lookAtEntity;
        private float m_entitySize;

        private MouseMoveMode m_mouseMoveMode = MouseMoveMode.None;
        private Point m_mouseStartPosition;
        private float m_yawRotation;
        private float m_pitchRotation;
        private float m_zoomAmount;
        private float m_zoomDelta;
        private Vector3 m_lookAtOffset = Vector3.Zero;

        public PerspectiveCamera(PBRRenderEngine engine, ResourceFactory factory)
        {
            m_projectionBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            m_projectionBuffer.Name = "CameraProjection_Buffer";

            m_viewBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            m_viewBuffer.Name = "CameraView_Buffer";

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
                1000f);
        }

        public void OnMouseDown(RenderViewControl.RenderViewControl control, MouseButtonEventArgs args)
        {
            m_mouseMoveMode = args.ChangedButton == MouseButton.Left
                ? MouseMoveMode.Orbit
                : MouseMoveMode.Pan;
            m_mouseStartPosition = args.GetPosition(control);
        }

        public void OnMouseUp(RenderViewControl.RenderViewControl control, MouseButtonEventArgs args)
        {
            m_mouseMoveMode = MouseMoveMode.None;
        }

        public void OnMouseMove(RenderViewControl.RenderViewControl control, MouseEventArgs args)
        {
            if (m_mouseMoveMode == MouseMoveMode.None)
            {
                return;
            }

            var currentMousePosition = args.GetPosition(control);
            var moveAmount = currentMousePosition - m_mouseStartPosition;
            m_mouseStartPosition = currentMousePosition;

            switch (m_mouseMoveMode)
            {
                case MouseMoveMode.Orbit:
                    m_yawRotation -= (float)moveAmount.X * 0.01f;
                    m_pitchRotation += (float)moveAmount.Y * 0.01f;
                    break;
                case MouseMoveMode.Pan:
                    // TODO: Support pan movement
                    break;
            }
        }

        public void OnMouseWheel(RenderViewControl.RenderViewControl sender, MouseWheelEventArgs args)
        {
            if (m_lookAtEntity == null)
            {
                return;
            }

            var delta = args.Delta * 0.001f;
            m_zoomDelta += (m_entitySize * delta);
        }

        public void OnKeyDown(RenderViewControl.RenderViewControl control, KeyEventArgs args)
        {
            if (m_lookAtEntity == null)
            {
                return;
            }

            if (args.Key == Key.OemPlus || args.Key == Key.Add)
            {
                m_zoomDelta += (m_entitySize * 0.01f);
            }
            else if (args.Key == Key.OemMinus || args.Key == Key.Subtract)
            {
                m_zoomDelta -= (m_entitySize * 0.01f);
            }
            else if (args.Key == Key.Home)
            {
                ResetCamera();
            }
        }

        public void OnKeyUp(RenderViewControl.RenderViewControl control, KeyEventArgs args)
        {
        }

        public void FixedUpdate()
        {
            var newZoom = m_zoomAmount + m_zoomDelta;
            m_zoomDelta *= 0.75f;

            if (newZoom <= -(m_entitySize * 0.5f))
            {
                m_zoomAmount = newZoom;
            }
        }

        public void GenerateCommands(CommandList commandList)
        {
            commandList.UpdateBuffer(m_projectionBuffer, 0, m_projectionMatrix);

            if (m_lookAtEntity != null)
            {
                var lookAtBounds = m_lookAtEntity.GetBoundingBox();
                var zOffset = Vector3.UnitZ * m_zoomAmount;
                var cameraLookAt = lookAtBounds.Center + m_lookAtOffset;
                var cameraPosition = Vector3.Transform(cameraLookAt + zOffset, Matrix4x4.CreateFromYawPitchRoll(m_yawRotation, m_pitchRotation, 0.0f));

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
            ResetCamera();
        }

        private void ResetCamera()
        {
            m_yawRotation = -0.5f;
            m_pitchRotation = -6.0f;
            m_zoomDelta = 0.0f;
            m_entitySize = m_lookAtEntity?.GetBoundingBox().Extent.Length() ?? 0.0f;
            m_zoomAmount = m_entitySize * -2.5f;
        }
    }
}