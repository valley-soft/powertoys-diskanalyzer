using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer
{
    public partial class DiskAnalyzerWindow : Window
    {
        public ObservableCollection<FolderNode> RootFolders { get; set; } = new ObservableCollection<FolderNode>();

        // Navigation history for back button
        private readonly Stack<string> _history = new Stack<string>();
        private string? _currentPath;

        public DiskAnalyzerWindow(string? rootPath)
        {
            InitializeComponent();
            DataContext = this;

            if (string.IsNullOrWhiteSpace(rootPath))
            {
                _ = LoadAllDrivesAsync();
            }
            else
            {
                _ = LoadTreeAsync(rootPath);
            }
        }

        private async Task LoadAllDrivesAsync()
        {
            RootFolders.Clear();
            try
            {
                var drives = await Task.Run(() =>
                    DriveInfo.GetDrives().Where(d => d.IsReady).ToList());

                foreach (var drive in drives)
                {
                    var node = new FolderNode { Name = drive.Name, FullPath = drive.Name };
                    // Dummy child so the expand arrow shows
                    node.Children.Add(new FolderNode { Name = "Loading...", FullPath = "" });
                    RootFolders.Add(node);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading drives: {ex.Message}");
            }
        }

        private async Task LoadTreeAsync(string path)
        {
            RootFolders.Clear();
            try
            {
                var rootNode = new FolderNode { Name = path, FullPath = path };
                rootNode.Children.Add(new FolderNode { Name = "Loading...", FullPath = "" });
                RootFolders.Add(rootNode);

                var children = await Task.Run(() => GetSubfolders(path));
                rootNode.Children.Clear();
                foreach (var c in children)
                    rootNode.Children.Add(c);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading path: {ex.Message}");
            }
        }

        private static List<FolderNode> GetSubfolders(string path)
        {
            var result = new List<FolderNode>();
            try
            {
                var dirInfo = new DirectoryInfo(path);
                foreach (var d in dirInfo.EnumerateDirectories())
                {
                    try
                    {
                        if ((d.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                            continue;
                        var node = new FolderNode { Name = d.Name, FullPath = d.FullName };
                        node.Children.Add(new FolderNode { Name = "Loading...", FullPath = "" });
                        result.Add(node);
                    }
                    catch { }
                }
            }
            catch { }
            return result.OrderBy(n => n.Name).ToList();
        }

        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TreeViewItem tvi && tvi.DataContext is FolderNode node)
            {
                if (node.Children.Count == 1 && node.Children[0].FullPath == "")
                {
                    node.Children.Clear();
                    var children = await Task.Run(() => GetSubfolders(node.FullPath));
                    foreach (var c in children)
                        node.Children.Add(c);
                }
            }
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FolderNode node && !string.IsNullOrEmpty(node.FullPath))
            {
                NavigateTo(node.FullPath);
            }
        }

        private void NavigateTo(string path, bool addToHistory = true)
        {
            if (addToHistory && _currentPath != null)
                _history.Push(_currentPath);

            _currentPath = path;
            CurrentPathText.Text = path;
            BackButton.IsEnabled = _history.Count > 0;
            _ = LoadGridDataAsync(path);
        }

        private async Task LoadGridDataAsync(string path)
        {
            ItemsGrid.ItemsSource = null;
            StatusText.Text = $"Scanning {path}...";

            try
            {
                var items = await Task.Run(() => DiskAnalyzerHelper.ScanDirectory(path, 1, false));

                var viewModels = items
                    .Select(i => new GridItemViewModel
                    {
                        Name = i.IsFile ? i.Name : "📁 " + i.Name,
                        FullPath = i.FullPath,
                        FormattedSize = DiskAnalyzerHelper.FormatSize(i.SizeBytes),
                        FormattedAllocated = DiskAnalyzerHelper.FormatSize(i.AllocatedSizeBytes),
                        ItemCount = i.ItemCount,
                        LastModified = i.LastModified,
                        SizeBytes = i.SizeBytes,
                        AllocatedSizeBytes = i.AllocatedSizeBytes,
                        IsFile = i.IsFile,
                    })
                    .OrderByDescending(v => v.SizeBytes)
                    .ToList();

                ItemsGrid.ItemsSource = viewModels;
                StatusText.Text = $"{viewModels.Count} item(s) in {path}  •  Double-click a folder to drill down";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        // Double-click on a grid row: drill into folder, or open file in Explorer
        private void ItemsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemsGrid.SelectedItem is GridItemViewModel vm)
            {
                if (vm.IsFile)
                {
                    // Open file location in Explorer
                    try
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{vm.FullPath}\"");
                    }
                    catch { }
                }
                else
                {
                    // Drill down into the folder
                    NavigateTo(vm.FullPath);
                }
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (_history.Count > 0)
            {
                var prev = _history.Pop();
                _currentPath = prev;
                CurrentPathText.Text = prev;
                BackButton.IsEnabled = _history.Count > 0;
                _ = LoadGridDataAsync(prev);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPath != null)
                _ = LoadGridDataAsync(_currentPath);
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select a folder to analyze"
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.FolderName))
            {
                _ = LoadTreeAsync(dialog.FolderName);
                NavigateTo(dialog.FolderName);
            }
        }
    }

    public class FolderNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public ObservableCollection<FolderNode> Children { get; set; } = new ObservableCollection<FolderNode>();
    }

    public class GridItemViewModel
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string FormattedSize { get; set; } = "";
        public string FormattedAllocated { get; set; } = "";
        public int ItemCount { get; set; }
        public DateTime LastModified { get; set; }
        public long SizeBytes { get; set; }
        public long AllocatedSizeBytes { get; set; }
        public bool IsFile { get; set; }
    }
}
