using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine.Shaders
{
    public interface IRenderShader
    {
        string GetVertexShaderPath();
        string GetPixelShaderPath();
        VertexLayoutDescription GetVertexLayout();
    }
}