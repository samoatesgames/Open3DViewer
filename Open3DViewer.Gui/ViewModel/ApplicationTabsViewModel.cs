using System;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Open3DViewer.Gui.PBRRenderEngine.Types;

namespace Open3DViewer.Gui.ViewModel
{
    public enum ApplicationTabs
    {
        EnvironmentAndLighting,
        StatsAndShading,
        GridAndViews
    }

    public class ShadingModeViewModel : ObservableObject
    {
        private readonly ApplicationTabsViewModel m_applicationTabsViewModel;

        public ShadingModes ShadingMode { get; }
        public string DisplayName { get; }

        public bool IsActive
        {
            get => m_applicationTabsViewModel.ActiveShadingMode == ShadingMode;
            set
            {
                if (value)
                {
                    m_applicationTabsViewModel.ActiveShadingMode = ShadingMode;
                }
                OnPropertyChanged();
            }
        }

        // TODO: We shouldn't need ApplicationTabsViewModel because the 'active shading mode' should be in a scene description object
        public ShadingModeViewModel(ApplicationTabsViewModel tabsViewModel, ShadingModes shadingMode)
        {
            m_applicationTabsViewModel = tabsViewModel;
            
            ShadingMode = shadingMode;
            DisplayName = shadingMode.ToString(); // TODO: Add a display name attribute to the enum
        }
    }

    public class ApplicationTabsViewModel : ObservableObject
    {
        private PBRRenderEngine.PBRRenderEngine m_engine;

        private ApplicationTabs m_activeTab = ApplicationTabs.EnvironmentAndLighting;
        private ShadingModes m_activeShadingMode = ShadingModes.Default; // TODO: This should be stored on a screen description in the render engine
        private Color m_ambientLightColor = Color.FromScRgb(1.0f, 0.8f, 0.7f, 0.7f);
        private Color m_directionalLightColor = Color.FromScRgb(1.0f, 1.0f, 1.0f, 1.0f);

        public ApplicationTabs ActiveTab
        {
            get => m_activeTab;
            set
            {
                if (m_activeTab != value)
                {
                    m_activeTab = value;
                    OnPropertyChanged();
                }
            }
        }

        public ShadingModes ActiveShadingMode
        {
            get => m_activeShadingMode;
            set
            {
                if (m_activeShadingMode != value)
                {
                    m_activeShadingMode = value;
                    OnPropertyChanged();

                    m_engine.ActiveScene.SetShadingMode(value);
                }
            }
        }

        public Color AmbientLightColor
        {
            get => m_ambientLightColor;
            set
            {
                if (m_ambientLightColor != value)
                {
                    m_ambientLightColor = value;
                    OnPropertyChanged();

                    m_engine.ActiveScene.SetAmbientLightColor(new Vector3(value.ScR, value.ScG, value.ScB));
                }
            }
        }

        public Color DirectionalLightColor
        {
            get => m_directionalLightColor;
            set
            {
                if (m_directionalLightColor != value)
                {
                    m_directionalLightColor = value;
                    OnPropertyChanged();

                    m_engine.ActiveScene.SetDirectionalLightColor(new Vector3(value.ScR, value.ScG, value.ScB));
                }
            }
        }

        public ObservableCollection<ShadingModeViewModel> SupportedRenderModes { get; } = new ObservableCollection<ShadingModeViewModel>();

        public ApplicationTabsViewModel(PBRRenderEngine.PBRRenderEngine engine)
        {
            m_engine = engine;

            foreach (ShadingModes shadingMode in Enum.GetValues(typeof(ShadingModes)))
            {
                SupportedRenderModes.Add(new ShadingModeViewModel(this, shadingMode));
            }
        }
    }
}
