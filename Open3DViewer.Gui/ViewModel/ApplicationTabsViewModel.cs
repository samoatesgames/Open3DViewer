using CommunityToolkit.Mvvm.ComponentModel;
using Open3DViewer.Gui.PBRRenderEngine.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Data;
using System.Windows.Media;

namespace Open3DViewer.Gui.ViewModel
{
    public enum ApplicationTabs
    {
        EnvironmentAndLighting,
        StatsAndShading,
        GridAndViews
    }
    
    public class ApplicationTabsViewModel : ObservableObject
    {
        private readonly PBRRenderEngine.PBRRenderEngine m_engine;
        private readonly List<ShadingModeViewModel> m_renderModes;

        private ApplicationTabs m_activeTab = ApplicationTabs.EnvironmentAndLighting;

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
            get => m_engine.SceneInfo.ShadingMode;
            set
            {
                if (m_engine.SceneInfo.ShadingMode != value)
                {
                    m_engine.SetShadingMode(value);
                    OnPropertyChanged();
                }
            }
        }

        public Color AmbientLightColor
        {
            get
            {
                var color = m_engine.SceneInfo.AmbientLightColor;
                return Color.FromScRgb(1.0f, color.X, color.Y, color.Z);
            }
            set
            {
                var color = new Vector3(value.ScR, value.ScG, value.ScB);
                if (m_engine.SceneInfo.AmbientLightColor != color)
                {
                    m_engine.SetAmbientLightColor(color);
                    OnPropertyChanged();
                }
            }
        }

        public Color DirectionalLightColor
        {
            get
            {
                var color = m_engine.SceneInfo.Lights[0].Radiance;
                return Color.FromScRgb(1.0f, color.X, color.Y, color.Z);
            }
            set
            {
                var color = new Vector3(value.ScR, value.ScG, value.ScB);
                if (m_engine.SceneInfo.Lights[0].Radiance != color)
                {
                    m_engine.SetDirectionalLightColor(color);
                    OnPropertyChanged();
                }
            }
        }

        public CollectionView SupportedRenderModes { get; }

        public ApplicationTabsViewModel(PBRRenderEngine.PBRRenderEngine engine)
        {
            m_engine = engine;

            m_renderModes = new List<ShadingModeViewModel>();
            foreach (ShadingModes shadingMode in Enum.GetValues(typeof(ShadingModes)))
            {
                m_renderModes.Add(new ShadingModeViewModel(engine, shadingMode));
            }

            SupportedRenderModes = (CollectionView)CollectionViewSource.GetDefaultView(m_renderModes);
            SupportedRenderModes.GroupDescriptions?.Add(new PropertyGroupDescription("Group"));
        }
    }
}
