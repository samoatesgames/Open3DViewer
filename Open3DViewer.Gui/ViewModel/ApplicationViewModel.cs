using System;

namespace Open3DViewer.Gui.ViewModel
{
    internal class ApplicationViewModel
    {
        private readonly PBRRenderEngine.PBRRenderEngine m_renderEngine;

        public ApplicationViewModel(PBRRenderEngine.PBRRenderEngine renderEngine)
        {
            m_renderEngine = renderEngine;
            renderEngine.OnInitialized += RenderEngineOnOnInitialized;
        }

        private void RenderEngineOnOnInitialized(PBRRenderEngine.PBRRenderEngine engine)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                engine.TryLoadAsset(args[1]);
            }
            engine.OnInitialized -= RenderEngineOnOnInitialized;
        }
    }
}
