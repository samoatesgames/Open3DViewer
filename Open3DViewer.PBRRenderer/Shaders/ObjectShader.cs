using System.IO;
using System.Runtime.CompilerServices;
using Veldrid;

namespace Open3DViewer.PBRRenderer.Shaders
{
    public class ObjectShader : IRenderShader
    {
        public string GetVertexShaderPath()
        {
#if DEBUG
            return $@"{GetProjectDirectory()}\..\Assets\PBRRenderer\Shaders\ObjectShader\ObjectShader.vert.glsl";
#else
            return @"Assets\PBRRenderer\Shaders\ObjectShader\ObjectShader.vert.glsl";
#endif
        }

        public string GetPixelShaderPath()
        {
#if DEBUG
            return $@"{GetProjectDirectory()}\..\Assets\PBRRenderer\Shaders\ObjectShader\ObjectShader.frag.glsl";
#else
            return @"Assets\PBRRenderer\Shaders\ObjectShader\ObjectShader.frag.glsl";
#endif
        }

#if DEBUG
        private static string GetProjectDirectory([CallerFilePath] string callerPath = "")
        {
            return Path.GetDirectoryName(callerPath);
        }
#endif

        public VertexLayoutDescription GetVertexLayout()
        {
            return new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Tangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("BiTangent", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2)
            );
        }
    }
}