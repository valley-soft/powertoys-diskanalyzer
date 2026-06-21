using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using ManagedCommon;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer
{
    public partial class DiskAnalyzerWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<FolderNode> RootFolders { get; set; } = new ObservableCollection<FolderNode>();
        public bool CanGoBack => _history.Count > 0;

        // Navigation history for back button
        private readonly Stack<string> _history = new Stack<string>();
        private CancellationTokenSource? _scanCts;
        private string? _currentPath;
        private Theme _theme;

        public DiskAnalyzerWindow(Theme theme, string? rootPath = null)
        {
            _theme = theme;
            ApplyTheme(theme);

            InitializeComponent();
            DataContext = this;

            if (!string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
            {
                _ = LoadTreeAsync(rootPath);
                NavigateTo(rootPath);
            }
            else
            {
                _ = LoadAllDrivesAsync();
            }
        }

        private void ApplyTheme(Theme theme)
        {
            string themeName = (theme == Theme.Dark || theme == Theme.HighContrastBlack) ? "Dark" : "Light";
            var dict = new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/Community.PowerToys.Run.Plugin.DiskAnalyzer;component/Themes/{themeName}.xaml")
            };
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(dict);
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
                    var node = new FolderNode { Name = drive.Name, FullPath = drive.Name, IconSource = IconUtilities.GetIcon(drive.Name, true) };
                    // Dummy child so the expand arrow shows
                    node.Children.Add(new FolderNode { Name = "Loading...", FullPath = "" });
                    RootFolders.Add(node);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading drives: {ex.Message}");
            }
            
            NavigateTo("This PC", addToHistory: false);
        }

        private async Task LoadTreeAsync(string path)
        {
            RootFolders.Clear();
            try
            {
                var rootNode = new FolderNode { Name = path, FullPath = path, IconSource = IconUtilities.GetIcon(path, true) };
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
                        var node = new FolderNode { Name = d.Name, FullPath = d.FullName, IconSource = IconUtilities.GetIcon(d.FullName, true) };
                        node.Children.Add(new FolderNode { Name = "Loading...", FullPath = "" });
                        result.Add(node);
                    }
                    catch { }
                }
            }
            catch { }
            return result.OrderBy(n => n.Name).ToList();
        }

        private async void FolderTree_Expanded(object sender, RoutedEventArgs e)
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
            if (BackButton != null) BackButton.IsEnabled = _history.Count > 0;
            _ = LoadGridDataAsync(path);
        }

        private async Task LoadGridDataAsync(string path)
        {
            if (path == "This PC" || string.IsNullOrWhiteSpace(path))
            {
                ItemsGrid.ItemsSource = null;
                StatusText.Text = "Loading drives...";
                try
                {
                    var drives = await Task.Run(() => DriveInfo.GetDrives().Where(d => d.IsReady).ToList());
                    var viewModels = drives.Select(drive =>
                    {
                        var used = drive.TotalSize - drive.AvailableFreeSpace;
                        return new GridItemViewModel
                        {
                            Name = drive.Name,
                            FullPath = drive.Name,
                            FormattedSize = DiskAnalyzerHelper.FormatSize(used),
                            FormattedAllocated = DiskAnalyzerHelper.FormatSize(used),
                            ItemCount = 0,
                            LastModified = DateTime.MinValue,
                            SizeBytes = used,
                            AllocatedSizeBytes = used,
                            IsFile = false,
                            IconSource = IconUtilities.GetIcon(drive.Name, true)
                        };
                    }).ToList();
                    ItemsGrid.ItemsSource = viewModels;
                    StatusText.Text = $"{viewModels.Count} drive(s) found  •  Double-click a drive to drill down";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error: {ex.Message}";
                }
                return;
            }

            ItemsGrid.ItemsSource = null;
            StatusText.Text = $"Scanning {path}...";

            try
            {
                var items = await Task.Run(() => DiskAnalyzerHelper.ScanDirectory(path, 1, true));

                var viewModels = items
                    .Select(i => new GridItemViewModel
                    {
                        Name = i.Name,
                        FullPath = i.FullPath,
                        FormattedSize = DiskAnalyzerHelper.FormatSize(i.SizeBytes),
                        FormattedAllocated = DiskAnalyzerHelper.FormatSize(i.AllocatedSizeBytes),
                        ItemCount = i.FileCount + i.FolderCount,
                        LastModified = i.LastModified,
                        SizeBytes = i.SizeBytes,
                        AllocatedSizeBytes = i.AllocatedSizeBytes,
                        IsFile = i.IsFile,
                        IconSource = IconUtilities.GetIcon(i.FullPath, !i.IsFile)
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
                if (BackButton != null) BackButton.IsEnabled = _history.Count > 0;
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

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (tag == "Light")
                    ApplyTheme(Theme.Light);
                else if (tag == "Dark")
                    ApplyTheme(Theme.Dark);
                else
                    ApplyTheme(_theme); // System (PowerToys default)
            }
        }
    }

    public class FolderNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public ImageSource? IconSource { get; set; }
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
        /// <summary>Returns a formatted date string, or empty for drives (DateTime.MinValue).</summary>
        public string FormattedDate =>
            LastModified == DateTime.MinValue ? "" : LastModified.ToString("M/d/yyyy h:mm:ss tt");
        public long SizeBytes { get; set; }
        public long AllocatedSizeBytes { get; set; }
        public bool IsFile { get; set; }
        public ImageSource? IconSource { get; set; }
    }
}
