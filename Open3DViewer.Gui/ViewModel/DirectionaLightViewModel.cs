using CommunityToolkit.Mvvm.ComponentModel;
using System.Numerics;
using System.Windows.Media;
using Open3DViewer.PBRRenderer;

namespace Open3DViewer.Gui.ViewModel
{
    public class DirectionalLightViewModel : ObservableObject
    {
        private readonly PBRRenderEngine m_engine;
        private readonly int m_lightIndex;

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
            get => m_engine.SceneInfo.Lights[m_lightIndex].IsActive == 1u;
            set
            {
                m_engine.SetDirectionalLightActive(m_lightIndex, value);
                OnPropertyChanged();
            }
        }

        public DirectionalLightViewModel(PBRRenderEngine engine, int lightIndex)
        {
            m_engine = engine;
            m_lightIndex = lightIndex;
        }
    }
}
