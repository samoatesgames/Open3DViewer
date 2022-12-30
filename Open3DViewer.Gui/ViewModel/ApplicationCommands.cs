using System;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Open3DViewer.Gui.ViewModel
{
    internal class ApplicationCommands : ObservableObject
    {
        private readonly PBRRenderEngine.PBRRenderEngine m_renderEngine;
        private readonly ICommand m_commandFileRecentOpen;
        private readonly Task m_loadRecentFileTask;

        public ObservableCollection<RecentFileViewModel> RecentFiles { get; } = new ObservableCollection<RecentFileViewModel>();

        public ICommand CommandFileOpen { get; }
        public ICommand CommandFileExit { get; }
        public ICommand CommandLoadExampleAsset { get; }

        public ApplicationCommands(PBRRenderEngine.PBRRenderEngine renderEngine)
        {
            m_renderEngine = renderEngine;

            m_commandFileRecentOpen = new AsyncRelayCommand<string>(OpenFileByPath);

            CommandFileOpen = new AsyncRelayCommand(HandleFileOpen);
            CommandFileExit = new RelayCommand(HandleFileExit);
            CommandLoadExampleAsset = new AsyncRelayCommand<string>(HandleLoadExampleAsset);

            m_loadRecentFileTask = LoadRecentFileFromFile();
        }

        private async Task AddFileToRecentFiles(string filePath)
        {
            var existing = RecentFiles.FirstOrDefault(x => x.FilePath == filePath);
            if (existing != null)
            {
                RecentFiles.Remove(existing);
                RecentFiles.Insert(0, existing);
                OnPropertyChanged(nameof(RecentFiles));
                return;
            }

            RecentFiles.Insert(0, new RecentFileViewModel(filePath, m_commandFileRecentOpen));
            while (RecentFiles.Count > 5)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }
            OnPropertyChanged(nameof(RecentFiles));

            await SaveRecentFilesToFile();
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
                        await AddFileToRecentFiles(filePath);
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
                    foreach (var recentFile in RecentFiles)
                    {
                        await writer.WriteLineAsync(recentFile.FilePath);
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
    }
}
