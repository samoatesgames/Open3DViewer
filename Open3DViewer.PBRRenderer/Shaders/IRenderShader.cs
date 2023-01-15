using Veldrid;

namespace Open3DViewer.PBRRenderer.Shaders
{
    public interface IRenderShader
    {
        string GetVertexShaderPath();
        string GetPixelShaderPath();
        VertexLayoutDescription GetVertexLayout();
    }
}