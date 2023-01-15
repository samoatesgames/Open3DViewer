using Open3DViewer.PBRRenderer.GLTF;
using Open3DViewer.PBRRenderer.Types;
using System.Numerics;
using System.Runtime.InteropServices;
using Open3DViewer.RenderViewControl.Types;
using Veldrid;

namespace Open3DViewer.PBRRenderer.Camera
{
    public class PerspectiveCamera
    {
        private enum MouseMoveMode
        {
            None,
            Orbit,
            Pan
        }

        private ViewProjectionInfo m_viewProjectionInfo;
        private readonly DeviceBuffer m_viewProjectionBuffer;

        private float m_aspectRatio;
        private GLTFEntity m_lookAtEntity;
        private float m_entitySize;

        private MouseMoveMode m_mouseMoveMode = MouseMoveMode.None;
        private Vector2 m_mouseStartPosition;
        private float m_yawRotation;
        private float m_pitchRotation;
        private float m_zoomAmount;
        private float m_zoomDelta;

        public Vector3 Position { get; private set; }

        public PerspectiveCamera(PBRRenderEngine engine, ResourceFactory factory)
        {
            m_viewProjectionBuffer = factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf<ViewProjectionInfo>(), BufferUsage.UniformBuffer));
            m_viewProjectionBuffer.Name = "CameraViewProjection_Buffer";

            engine.RegisterSharedResource(CoreSharedResource.ViewProjectionBuffer, m_viewProjectionBuffer);

            var frameBuffer = engine.Swapchain.Framebuffer;
            OnSwapchainResized(frameBuffer.Width, frameBuffer.Height);
        }

        public void OnSwapchainResized(uint width, uint height)
        {
            m_aspectRatio = (float)width / height;
            m_viewProjectionInfo.Projection = Matrix4x4.CreatePerspectiveFieldOfView(
                1.0f,
                m_aspectRatio,
                0.005f,
                1000f);
        }

        public void OnMouseDown(MouseButtonInfo args)
        {
            m_mouseMoveMode = args.PressedButton == PressedMouseButton.Left
                ? MouseMoveMode.Orbit
                : MouseMoveMode.Pan;

            m_mouseStartPosition = args.Position;
        }

        public void OnMouseUp(MouseButtonInfo args)
        {
            m_mouseMoveMode = MouseMoveMode.None;
        }

        public void OnMouseMove(MouseMoveInfo args)
        {
            if (m_mouseMoveMode == MouseMoveMode.None)
            {
                return;
            }

            var currentMousePosition = args.Position;
            var moveAmount = currentMousePosition - m_mouseStartPosition;
            m_mouseStartPosition = currentMousePosition;

            switch (m_mouseMoveMode)
            {
                case MouseMoveMode.Orbit:
                    m_yawRotation -= moveAmount.X * 0.01f;
                    m_pitchRotation += moveAmount.Y * 0.01f;
                    break;
                case MouseMoveMode.Pan:
                    // TODO: Support pan movement
                    break;
            }
        }

        public void OnMouseWheel(MouseWheelInfo args)
        {
            if (m_lookAtEntity == null)
            {
                return;
            }

            var delta = args.WheelAmount * 0.001f;
            m_zoomDelta += (m_entitySize * delta);
        }

        public void OnKeyDown(KeyPressInfo args)
        {
            if (m_lookAtEntity == null)
            {
                return;
            }

            if (args.Key == '+')
            {
                ZoomIn();
            }
            else if (args.Key == '-')
            {
                ZoomOut();
            }
            else if (args.Key == KeyPressInfo.KeyHome)
            {
                ResetCamera();
            }
        }

        public void OnKeyUp(KeyPressInfo args)
        {
        }

        public void ZoomIn()
        {
            m_zoomDelta -= (m_entitySize * 0.01f);
        }

        public void ZoomOut()
        {
            m_zoomDelta += (m_entitySize * 0.01f);
        }

        public void FixedUpdate()
        {
            var newZoom = m_zoomAmount + m_zoomDelta;
            m_zoomDelta *= 0.75f;

            if (newZoom > (m_entitySize * 0.25f))
            {
                m_zoomAmount = newZoom;
            }
        }

        public void GenerateCommands(CommandList commandList)
        {
            if (m_lookAtEntity != null)
            {
                var lookAtBounds = m_lookAtEntity.GetBoundingBox();
                var zOffset = Vector3.UnitZ * m_zoomAmount;
                var cameraLookAt = lookAtBounds.Center;
                Position = Vector3.Transform(cameraLookAt + zOffset, Matrix4x4.CreateFromYawPitchRoll(m_yawRotation, m_pitchRotation, 0.0f));
                m_viewProjectionInfo.View = Matrix4x4.CreateLookAt(Position, cameraLookAt, Vector3.UnitY);
            }
            else
            {
                Position = Vector3.UnitZ * 5f;
                m_viewProjectionInfo.View = Matrix4x4.CreateLookAt(Position, Vector3.Zero, Vector3.UnitY);
            }

            commandList.UpdateBuffer(m_viewProjectionBuffer, 0, ref m_viewProjectionInfo);
        }

        public void LookAt(GLTFEntity entity)
        {
            m_lookAtEntity = entity;
            ResetCamera();
        }

        public void ResetCamera()
        {
            Position = Vector3.UnitZ * 5f;
            m_yawRotation = -6.6f;
            m_pitchRotation = 6.1f;
            m_zoomDelta = 0.0f;
            m_entitySize = m_lookAtEntity?.GetBoundingBox().Extent.Length() ?? 0.0f;
            m_zoomAmount = m_entitySize * 2.5f;
        }
    }
}