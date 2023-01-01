using CommunityToolkit.Mvvm.ComponentModel;

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
    }
}
