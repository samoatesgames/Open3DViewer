using System;
using System.Threading.Tasks;

namespace Open3DViewer.Gui.ViewModel
{
    internal class ApplicationViewModel
    {
        public ApplicationTabsViewModel Tabs { get; }
        public ApplicationCommands Commands { get; }

        public ApplicationViewModel(PBRRenderEngine.PBRRenderEngine renderEngine, RenderViewControl.RenderViewControl renderViewControl)
        {
            renderEngine.OnInitialized += RenderEngineOnOnInitialized;
            Tabs = new ApplicationTabsViewModel();
            Commands = new ApplicationCommands(renderEngine, renderViewControl, Tabs);
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
