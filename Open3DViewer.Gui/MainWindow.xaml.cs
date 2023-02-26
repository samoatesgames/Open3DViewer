using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Open3DViewer.Gui.ViewModel;
using Open3DViewer.PBRRenderer;

namespace Open3DViewer.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly PBRRenderEngine m_renderEngine;

        public MainWindow()
        {
            InitializeComponent();

            if (OperatingSystem.IsWindowsVersionAtLeast(7))
            {
                Wpf.Ui.Appearance.Accent.ApplySystemAccent();
            }

            m_renderEngine = new PBRRenderEngine();
            RenderView.RenderEngine = m_renderEngine;

            DataContext = new ApplicationViewModel(m_renderEngine, RenderView);
        }

        private bool TryGetDraggedGltfPath(DragEventArgs e, out string filePath)
        {
            try
            {
                if (!(e.Data.GetData(DataFormats.FileDrop, false) is string[] fileList))
                {
                    filePath = default;
                    return false;
                }

                filePath = fileList.FirstOrDefault();
                if (filePath == null)
                {
                    filePath = default;
                    return false;
                }

                var extension = Path.GetExtension(filePath);
                if (extension.Equals(".glb", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".gltf", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                // ignored
            }

            filePath = default;
            return false;
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (!TryGetDraggedGltfPath(e, out var filePath))
            {
                return;
            }

            Task.Run(async () =>
            {
                await m_renderEngine.TryLoadAssetAsync(filePath);
            });
        }

        private void MainWindow_OnDragOver(object sender, DragEventArgs e)
        {
            if (!TryGetDraggedGltfPath(e, out _))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }
    }
}