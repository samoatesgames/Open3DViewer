using System.IO;
using System.Runtime.CompilerServices;
using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine.Shaders
{
    public class GridShader : IRenderShader
    {
        public string GetVertexShaderPath()
        {
#if DEBUG
            return $@"{GetProjectDirectory()}\Assets\GridShader\GridShader.vert.glsl";
#else
            return @"PBRRenderEngine\Shaders\Assets\GridShader\GridShader.vert.glsl";
#endif
        }

        public string GetPixelShaderPath()
        {
#if DEBUG
            return $@"{GetProjectDirectory()}\Assets\GridShader\GridShader.frag.glsl";
#else
            return @"PBRRenderEngine\Shaders\Assets\GridShader\GridShader.frag.glsl";
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
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3)
            );
        }
    }
}
