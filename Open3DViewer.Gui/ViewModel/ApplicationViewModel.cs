using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Open3DViewer.Gui.ViewModel
{
    internal class ApplicationViewModel
    {
        private readonly PBRRenderEngine.PBRRenderEngine m_renderEngine;

        public ApplicationCommands Commands { get; }

        public ApplicationViewModel(PBRRenderEngine.PBRRenderEngine renderEngine)
        {
            m_renderEngine = renderEngine;
            renderEngine.OnInitialized += RenderEngineOnOnInitialized;

            Commands = new ApplicationCommands(renderEngine);
        }

        private void RenderEngineOnOnInitialized(PBRRenderEngine.PBRRenderEngine engine)
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                Task.Run(async () =>
                {
                    if (!await engine.TryLoadAssetAsync(args[1]))
                    {
                        // TODO: Show error message to user
                    }
                });
            }
            engine.OnInitialized -= RenderEngineOnOnInitialized;
        }
    }
}
