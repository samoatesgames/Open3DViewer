using CommunityToolkit.Mvvm.ComponentModel;
using Open3DViewer.Gui.PBRRenderEngine.Types;

namespace Open3DViewer.Gui.ViewModel
{
    public class ShadingModeViewModel : ObservableObject
    {
        private readonly PBRRenderEngine.PBRRenderEngine m_engine;

        public ShadingModes ShadingMode { get; }
        public string DisplayName { get; }

        public bool IsActive
        {
            get => m_engine.SceneInfo.ShadingMode == ShadingMode;
            set
            {
                if (value)
                {
                    m_engine.SetShadingMode(ShadingMode);
                }
                OnPropertyChanged();
            }
        }

        public ShadingModeViewModel(PBRRenderEngine.PBRRenderEngine engine, ShadingModes shadingMode)
        {
            m_engine = engine;
            ShadingMode = shadingMode;
            DisplayName = shadingMode.ToString(); // TODO: Add a display name attribute to the enum
        }
    }
}
