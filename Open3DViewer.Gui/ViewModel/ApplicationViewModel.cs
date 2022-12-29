using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Open3DViewer.Gui.ViewModel
{
    internal class ApplicationViewModel
    {
        private readonly PBRRenderEngine.PBRRenderEngine m_renderEngine;

        public ICommand CommandLoadExampleAsset { get; }

        public ApplicationViewModel(PBRRenderEngine.PBRRenderEngine renderEngine)
        {
            m_renderEngine = renderEngine;
            renderEngine.OnInitialized += RenderEngineOnOnInitialized;

            CommandLoadExampleAsset = new AsyncRelayCommand<string>(HandleLoadExampleAsset);
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

        private async Task HandleLoadExampleAsset(string exampleAssetPath)
        {
            if (!await m_renderEngine.TryLoadAssetAsync(exampleAssetPath))
            {
                // TODO: Show error message to user
            }
        }
    }
}
