using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Open3DViewer.Gui.PBRRenderEngine.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DirectionalLight
    {
        public const uint ClassSize = 32;

        public Vector3 Direction;
        public uint IsActive;

        public Vector3 Radiance;
        public float Padding2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SceneInfo
    {
        public const uint LightCount = 3;
        public const uint ClassSize = (3 * DirectionalLight.ClassSize) + 48;

        public DirectionalLight[] Lights;

        public Vector3 AmbientLightColor;
        public float Padding0;

        public Vector3 CameraPosition;
        public float Padding3;

        public ShadingModes ShadingMode;
        public uint Padding4;
        public uint Padding5;
        public uint Padding6;

        public Span<byte> ToSpan()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (var light in Lights)
                    {
                        writer.Write(light.Direction.X);
                        writer.Write(light.Direction.Y);
                        writer.Write(light.Direction.Z);
                        writer.Write(light.IsActive);

                        writer.Write(light.Radiance.X);
                        writer.Write(light.Radiance.Y);
                        writer.Write(light.Radiance.Z);
                        writer.Write(0.0f);
                    }

                    writer.Write(AmbientLightColor.X);
                    writer.Write(AmbientLightColor.Y);
                    writer.Write(AmbientLightColor.Z);
                    writer.Write(0.0f);

                    writer.Write(CameraPosition.X);
                    writer.Write(CameraPosition.Y);
                    writer.Write(CameraPosition.Z);
                    writer.Write(0.0f);

                    writer.Write((uint)ShadingMode);
                    writer.Write((uint)0);
                    writer.Write((uint)0);
                    writer.Write((uint)0);
                }
                
                return new Span<byte>(stream.ToArray());
            }
        }

        public static SceneInfo Create()
        {
            var lights = new[]
            {
                new DirectionalLight
                {
                    Direction = Vector3.Normalize(new Vector3(0.0f,  0.4f, 1.0f)),
                    Radiance = Vector3.One * 0.5f,
                    IsActive = 1
                },
                new DirectionalLight
                {
                    Direction = Vector3.Normalize(new Vector3(-0.8f,  0.4f, -0.2f)),
                    Radiance = Vector3.One * 0.5f,
                    IsActive = 1
                },
                new DirectionalLight
                {
                    Direction = Vector3.Normalize(new Vector3(0.8f, 0.4f, -0.2f)),
                    Radiance = Vector3.One * 0.5f,
                    IsActive = 1
                }
            };

            return new SceneInfo
            {
                Lights = lights,
                AmbientLightColor = new Vector3(0.7f, 0.7f, 0.7f),
                Padding0 = 0.0f,
                CameraPosition = Vector3.UnitZ,
                Padding3 = 0.0f,
                ShadingMode = ShadingModes.Default
            };
        }
    }
}