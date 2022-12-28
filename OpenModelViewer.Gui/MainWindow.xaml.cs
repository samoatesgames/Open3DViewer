namespace OpenModelViewer.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            RenderView.RenderEngine = new PBRRenderEngine.PBRRenderEngine();
        }
    }
}