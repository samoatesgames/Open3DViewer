using Veldrid;

namespace OpenModelViewer.Gui.PBRRenderEngine.Shaders
{
    public interface IRenderShader
    {
        byte[] GetVertexShader();
        byte[] GetPixelShader();
        VertexLayoutDescription GetVertexLayout();
    }
}