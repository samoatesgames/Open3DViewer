using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Open3DViewer.Gui.ViewModel
{
    internal class ApplicationCommands : ObservableObject
    {
        private readonly PBRRenderEngine.PBRRenderEngine m_renderEngine;
        private readonly RenderViewControl.RenderViewControl m_renderViewControl;
        private readonly ICommand m_commandFileRecentOpen;

        public ObservableCollection<RecentFileViewModel> RecentFiles { get; } = new ObservableCollection<RecentFileViewModel>();

        public ICommand CommandFileOpen { get; }
        public ICommand CommandFileSaveAs { get; }
        public ICommand CommandFileExportImage { get; }
        public ICommand CommandFileExit { get; }
        public ICommand CommandLoadExampleAsset { get; }

        public ICommand CommandViewZoomIn { get; }
        public ICommand CommandViewZoomOut { get; }
        public ICommand CommandViewResetCamera { get; }

        public ApplicationCommands(PBRRenderEngine.PBRRenderEngine renderEngine, RenderViewControl.RenderViewControl renderViewControl)
        {
            m_renderEngine = renderEngine;
            m_renderViewControl = renderViewControl;

            m_commandFileRecentOpen = new AsyncRelayCommand<string>(OpenFileByPath);

            CommandFileOpen = new AsyncRelayCommand(HandleFileOpen);
            CommandFileSaveAs = new AsyncRelayCommand(HandleFileSaveAs);
            CommandFileExportImage = new AsyncRelayCommand(HandleFileExportImage);
            CommandFileExit = new RelayCommand(HandleFileExit);
            CommandLoadExampleAsset = new AsyncRelayCommand<string>(HandleLoadExampleAsset);

            CommandViewZoomIn = new RelayCommand(HandleViewZoomIn);
            CommandViewZoomOut = new RelayCommand(HandleViewZoomOut);
            CommandViewResetCamera = new RelayCommand(HandleViewResetCamera);

            _ = LoadRecentFileFromFile();
        }

        private async Task AddFileToRecentFiles(string filePath, bool saveToDisk = true)
        {
            var existing = RecentFiles.FirstOrDefault(x => x.FilePath == filePath);
            if (existing != null)
            {
                RecentFiles.Remove(existing);
                RecentFiles.Insert(0, existing);
                OnPropertyChanged(nameof(RecentFiles));

                if (saveToDisk)
                {
                    await SaveRecentFilesToFile();
                }
                return;
            }

            RecentFiles.Insert(0, new RecentFileViewModel(filePath, m_commandFileRecentOpen));
            while (RecentFiles.Count > 5)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
            OnPropertyChanged(nameof(RecentFiles));

            if (saveToDisk)
            {
                await SaveRecentFilesToFile();
            }
        }

        private string GetRecentFilesPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var folder = Path.Combine(appData, "Open3DViewer");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "RecentFiles.txt");
        }

        private async Task LoadRecentFileFromFile()
        {
            if (!File.Exists(GetRecentFilesPath()))
            {
                return;
            }

            using (var fileStream = new FileStream(GetRecentFilesPath(),
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    string filePath;
                    while ((filePath = await reader.ReadLineAsync()) != null)
                    {
                        if (!File.Exists(filePath))
                        {
                            continue;
                        }
                        await AddFileToRecentFiles(filePath, false);
                    }
                }
            }
        }

        private async Task SaveRecentFilesToFile()
        {
            using (var fileStream = new FileStream(GetRecentFilesPath(), 
                       FileMode.Create, 
                       FileAccess.Write,
                       FileShare.ReadWrite))
            {
                using (var writer = new StreamWriter(fileStream))
                {
                    var toSave = new List<string>();
                    toSave.AddRange(RecentFiles.Select(x => x.FilePath));
                    toSave.Reverse();

                    foreach (var recentFile in toSave)
                    {
                        await writer.WriteLineAsync(recentFile);
                    }
                }
            }
        }

        private async Task HandleFileOpen()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select GLTF or GLB to open...",
                Filter = "GLTF files (*.gltf;*.glb)|*.gltf;*.glb",
                Multiselect = false
            };
            if (openFileDialog.ShowDialog(Application.Current.MainWindow) != true)
            {
                return;
            }

            await OpenFileByPath(openFileDialog.FileName);
        }

        private async Task HandleFileSaveAs()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Select a location to save your GLB to...",
                Filter = "GLB file (*.glb)|*.glb",
                OverwritePrompt = true
            };
            if (saveFileDialog.ShowDialog(Application.Current.MainWindow) != true)
            {
                return;
            }

            var model = m_renderEngine.ActiveScene?.ModelRoot;
            if (model == null)
            {
                // TODO: Show error to user as nothing is currently loaded.
                return;
            }

            model.SaveGLB(saveFileDialog.FileName);

            await OpenFileByPath(saveFileDialog.FileName);
        }

        private async Task HandleFileExportImage()
        {
            using (var memoryStream = m_renderViewControl.TakeScreenshot())
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Select a location to save your image to...",
                    Filter = "PNG image (*.png)|*.png",
                    OverwritePrompt = true
                };
                if (saveFileDialog.ShowDialog(Application.Current.MainWindow) != true)
                {
                    return;
                }

                using (var fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    await memoryStream.CopyToAsync(fileStream);
                }

                Process.Start(saveFileDialog.FileName);
            }
        }

        private void HandleFileExit()
        {
            Environment.Exit(0);
        }

        private async Task OpenFileByPath(string filePath)
        {
            if (!await m_renderEngine.TryLoadAssetAsync(filePath))
            {
                // TODO: Show error message to user
                return;
            }
            await AddFileToRecentFiles(filePath);
        }

        private async Task HandleLoadExampleAsset(string exampleAssetPath)
        {
            if (!await m_renderEngine.TryLoadAssetAsync(exampleAssetPath))
            {
                // TODO: Show error message to user
            }
        }

        private void HandleViewZoomIn()
        {
            m_renderEngine.Camera.ZoomIn();
        }

        private void HandleViewZoomOut()
        {
            m_renderEngine.Camera.ZoomOut();
        }

        private void HandleViewResetCamera()
        {
            m_renderEngine.Camera.ResetCamera();
        }
    }
}
