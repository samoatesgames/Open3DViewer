using Veldrid;

namespace Open3DViewer.Gui.PBRRenderEngine.Shaders
{
    public interface IRenderShader
    {
        byte[] GetVertexShader();
        byte[] GetPixelShader();
        VertexLayoutDescription GetVertexLayout();
    }
}