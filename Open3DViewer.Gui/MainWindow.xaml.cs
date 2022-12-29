using Open3DViewer.Gui.ViewModel;

namespace Open3DViewer.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var renderEngine = new PBRRenderEngine.PBRRenderEngine();
            RenderView.RenderEngine = renderEngine;

            DataContext = new ApplicationViewModel(renderEngine);
        }
    }
}