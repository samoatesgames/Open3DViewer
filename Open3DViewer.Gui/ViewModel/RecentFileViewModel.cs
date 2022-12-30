using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Input;

namespace Open3DViewer.Gui.ViewModel
{
    internal class RecentFileViewModel : ObservableObject
    {
        public string FilePath { get; }
        public string FileName { get; }
        public ICommand Command { get; }

        public RecentFileViewModel(string filePath, ICommand openFileCommand)
        {
            FilePath = filePath;
            FileName = Path.GetFileNameWithoutExtension(filePath).ToUpper();
            Command = openFileCommand;
        }
    }
}
