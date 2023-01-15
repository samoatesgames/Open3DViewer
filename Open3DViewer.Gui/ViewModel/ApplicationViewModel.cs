using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Open3DViewer.PBRRenderer;

namespace Open3DViewer.Gui.ViewModel
{
    internal class ApplicationViewModel : ObservableObject
    {
        private readonly PBRRenderEngine m_engine;

        public ApplicationTabsViewModel Tabs { get; }
        public ApplicationCommands Commands { get; }

        public bool IsGridEnabled
        {
            get => m_engine.IsGridVisible;
            set => m_engine.SetGridVisible(value);
        }

        public bool ShowRenderView => !m_engine.IsAssetLoading;

        public ApplicationViewModel(PBRRenderEngine renderEngine, RenderViewControl.RenderViewControl renderViewControl)
        {
            m_engine = renderEngine;
            m_engine.OnInitialized += RenderEngineOnOnInitialized;
            m_engine.OnGridVisibilityChanged += RenderEngineOnOnGridVisibilityChanged;
            m_engine.OnAssetLoadingChanged += RenderEngineAssetLoadingChanged;

            Tabs = new ApplicationTabsViewModel(renderEngine);
            Commands = new ApplicationCommands(renderEngine, renderViewControl, Tabs);
        }

        private void RenderEngineOnOnInitialized(PBRRenderEngine engine)
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

        private void RenderEngineOnOnGridVisibilityChanged(PBRRenderEngine engine, bool visible)
        {
            OnPropertyChanged(nameof(IsGridEnabled));
        }

        private void RenderEngineAssetLoadingChanged(PBRRenderEngine engine, bool isLoading)
        {
            OnPropertyChanged(nameof(ShowRenderView));
        }
    }
}
