using SharpGLTF.Schema2;
using System.Numerics;
using Vortice.Mathematics;

namespace Open3DViewer.PBRRenderer.Camera
{
    internal class CameraFrustum
    {
        private const int PlaneCount = 6;
        private const int CornerCount = 8;

        private readonly PerspectiveCamera m_camera;
        private readonly Vector3[] m_corners = new Vector3[CornerCount];
        private readonly Plane[] m_planes = new Plane[PlaneCount];

        private Matrix4x4 m_matrix;

        public CameraFrustum(PerspectiveCamera camera)
        {
            m_camera = camera;
            m_matrix = m_camera.ViewMatrix * m_camera.ProjectionMatrix;
        }

        public void Update()
        {
            m_matrix = m_camera.ViewMatrix * m_camera.ProjectionMatrix;
            CreatePlanes();
            CreateCorners();
        }

        public bool Contains(BoundingBox box)
        {
            for (var i = 0; i < PlaneCount; ++i)
            {
                var planeIntersectionType = box.Intersects(ref m_planes[i]);
                switch (planeIntersectionType)
                {
                    case PlaneIntersectionType.Front:
                        return false;
                }
            }
            return true;
        }

        private void CreateCorners()
        {
            IntersectionPoint(ref m_planes[0], ref m_planes[2], ref m_planes[4], out m_corners[0]);
            IntersectionPoint(ref m_planes[0], ref m_planes[3], ref m_planes[4], out m_corners[1]);
            IntersectionPoint(ref m_planes[0], ref m_planes[3], ref m_planes[5], out m_corners[2]);
            IntersectionPoint(ref m_planes[0], ref m_planes[2], ref m_planes[5], out m_corners[3]);
            IntersectionPoint(ref m_planes[1], ref m_planes[2], ref m_planes[4], out m_corners[4]);
            IntersectionPoint(ref m_planes[1], ref m_planes[3], ref m_planes[4], out m_corners[5]);
            IntersectionPoint(ref m_planes[1], ref m_planes[3], ref m_planes[5], out m_corners[6]);
            IntersectionPoint(ref m_planes[1], ref m_planes[2], ref m_planes[5], out m_corners[7]);
        }

        private void CreatePlanes()
        {
            m_planes[0] = new Plane(-this.m_matrix.M13, -this.m_matrix.M23, -this.m_matrix.M33, -this.m_matrix.M43);
            m_planes[1] = new Plane(this.m_matrix.M13 - this.m_matrix.M14, this.m_matrix.M23 - this.m_matrix.M24, this.m_matrix.M33 - this.m_matrix.M34, this.m_matrix.M43 - this.m_matrix.M44);
            m_planes[2] = new Plane(-this.m_matrix.M14 - this.m_matrix.M11, -this.m_matrix.M24 - this.m_matrix.M21, -this.m_matrix.M34 - this.m_matrix.M31, -this.m_matrix.M44 - this.m_matrix.M41);
            m_planes[3] = new Plane(this.m_matrix.M11 - this.m_matrix.M14, this.m_matrix.M21 - this.m_matrix.M24, this.m_matrix.M31 - this.m_matrix.M34, this.m_matrix.M41 - this.m_matrix.M44);
            m_planes[4] = new Plane(this.m_matrix.M12 - this.m_matrix.M14, this.m_matrix.M22 - this.m_matrix.M24, this.m_matrix.M32 - this.m_matrix.M34, this.m_matrix.M42 - this.m_matrix.M44);
            m_planes[5] = new Plane(-this.m_matrix.M14 - this.m_matrix.M12, -this.m_matrix.M24 - this.m_matrix.M22, -this.m_matrix.M34 - this.m_matrix.M32, -this.m_matrix.M44 - this.m_matrix.M42);

            NormalizePlane(ref m_planes[0]);
            NormalizePlane(ref m_planes[1]);
            NormalizePlane(ref m_planes[2]);
            NormalizePlane(ref m_planes[3]);
            NormalizePlane(ref m_planes[4]);
            NormalizePlane(ref m_planes[5]);
        }

        private static void IntersectionPoint(ref Plane a, ref Plane b, ref Plane c, out Vector3 result)
        {
            // Formula used
            //                d1 ( N2 * N3 ) + d2 ( N3 * N1 ) + d3 ( N1 * N2 )
            //P =   -------------------------------------------------------------------------
            //                             N1 . ( N2 * N3 )
            //
            // Note: N refers to the normal, d refers to the displacement. '.' means dot product. '*' means cross product


            var cross = Vector3.Cross(b.Normal, c.Normal);

            var f = Vector3.Dot(a.Normal, cross);
            f *= -1.0f;

            var v1 = a.D * (Vector3.Cross(b.Normal, c.Normal));
            var v2 = (b.D * (Vector3.Cross(c.Normal, a.Normal)));
            var v3 = (c.D * (Vector3.Cross(a.Normal, b.Normal)));

            result.X = (v1.X + v2.X + v3.X) / f;
            result.Y = (v1.Y + v2.Y + v3.Y) / f;
            result.Z = (v1.Z + v2.Z + v3.Z) / f;
        }

        private void NormalizePlane(ref Plane p)
        {
            float factor = 1f / p.Normal.Length();
            p.Normal.X *= factor;
            p.Normal.Y *= factor;
            p.Normal.Z *= factor;
            p.D *= factor;
        }
    }
}
