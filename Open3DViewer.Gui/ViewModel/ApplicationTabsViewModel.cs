using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Windows.Data;
using System.Windows.Media;
using Open3DViewer.PBRRenderer;
using Open3DViewer.PBRRenderer.Types;

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
        private readonly PBRRenderEngine m_engine;

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
        
        public CollectionView SupportedRenderModes { get; }

        public ObservableCollection<DirectionalLightViewModel> DirectionalLights { get; } =
            new ObservableCollection<DirectionalLightViewModel>();

        public ApplicationTabsViewModel(PBRRenderEngine engine)
        {
            m_engine = engine;

            var renderModes = new List<ShadingModeViewModel>();
            foreach (ShadingModes shadingMode in Enum.GetValues(typeof(ShadingModes)))
            {
                renderModes.Add(new ShadingModeViewModel(engine, shadingMode));
            }

            SupportedRenderModes = (CollectionView)CollectionViewSource.GetDefaultView(renderModes);
            SupportedRenderModes.GroupDescriptions?.Add(new PropertyGroupDescription("Group"));

            for (var lightIndex = 0; lightIndex < engine.SceneInfo.Lights.Length; ++lightIndex)
            {
                DirectionalLights.Add(new DirectionalLightViewModel(engine, lightIndex));
            }
        }
    }
}
