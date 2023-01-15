using CommunityToolkit.Mvvm.ComponentModel;
using System.Numerics;
using System.Windows.Media;

namespace Open3DViewer.Gui.ViewModel
{
    public class DirectionalLightViewModel : ObservableObject
    {
        private readonly PBRRenderEngine.PBRRenderEngine m_engine;
        private readonly int m_lightIndex;

        private bool m_isActive = true;
        private Color m_activeLightColor;

        public string LightName => $"Directional Light {m_lightIndex + 1}";

        public Color LightColor
        {
            get
            {
                var color = m_engine.SceneInfo.Lights[m_lightIndex].Radiance;
                return Color.FromScRgb(1.0f, color.X, color.Y, color.Z);
            }
            set
            {
                var color = new Vector3(value.ScR, value.ScG, value.ScB);
                if (m_engine.SceneInfo.Lights[m_lightIndex].Radiance != color)
                {
                    m_engine.SetDirectionalLightColor(m_lightIndex, color);
                    OnPropertyChanged();
                }
            }
        }

        public bool IsActive
        {
            get => m_isActive;
            set
            {
                if (value != m_isActive)
                {
                    m_isActive = value;
                    if (m_isActive)
                    {
                        LightColor = m_activeLightColor;
                    }
                    else
                    {
                        m_activeLightColor = LightColor;
                        LightColor = Colors.Black;
                    }

                    OnPropertyChanged();
                }
            }
        }

        public DirectionalLightViewModel(PBRRenderEngine.PBRRenderEngine engine, int lightIndex)
        {
            m_engine = engine;
            m_lightIndex = lightIndex;
            m_activeLightColor = LightColor;
        }
    }
}
