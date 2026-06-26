using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Community.PowerToys.Run.Plugin.DiskAnalyzer;
using WinRT.Interop;

namespace ValleySoft_DiskAnalyzer_App
{
    public sealed partial class MainPage : Page
    {
        private ObservableCollection<GridItemViewModel> _currentItems = new ObservableCollection<GridItemViewModel>();
        private ObservableCollection<string> _pathSegments = new ObservableCollection<string>();
        private string _currentPath = string.Empty;
        private string _sortColumn = "Size";
        private bool _sortAscending = false;
        private bool _showHiddenFiles = true;
        private System.Threading.CancellationTokenSource? _navigationCts;

        public MainPage()
        {
            try
            {
                this.InitializeComponent();
                ResultsGrid.ItemsSource = _currentItems;
                PathBreadcrumbBar.ItemsSource = _pathSegments;
                _ = LoadDrivesAsync();
            }
            catch (Exception ex)
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "crash_mainpage.txt"),
                    ex.ToString());
            }

            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                bool alwaysAdmin = localSettings.Values["AlwaysRunAsAdmin"] as bool? ?? false;
                RunAsAdminToggle.IsChecked = alwaysAdmin;

                bool showWarning = localSettings.Values["ShowAdminWarning"] as bool? ?? true;
                ShowAdminWarningToggle.IsChecked = showWarning;
                
                if (showWarning && !IsAdministrator())
                {
                    AdminWarningBar.IsOpen = true;
                }
                else
                {
                    AdminWarningBar.IsOpen = false;
                }
            }
            catch { }
        }

        private static bool IsAdministrator()
        {
            using (System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }

        private async void RunAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            bool wantsAdmin = RunAsAdminToggle.IsChecked;
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["AlwaysRunAsAdmin"] = wantsAdmin;
            }
            catch { }

            if (wantsAdmin && !IsAdministrator())
            {
                var dialog = new ContentDialog
                {
                    Title = "Restart Required",
                    Content = "The application needs to restart to apply Administrator privileges. Restart now?",
                    PrimaryButtonText = "Restart",
                    CloseButtonText = "Later",
                    XamlRoot = this.XamlRoot
                };
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    RestartAsAdmin();
                }
            }
        }

        private void RestartAsAdmin()
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = System.Environment.CurrentDirectory,
                FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "ValleySoft.DiskAnalyzer.exe",
                Verb = "runas"
            };
            try
            {
                System.Diagnostics.Process.Start(startInfo);
                Application.Current.Exit();
            }
            catch
            {
                // User cancelled UAC
                RunAsAdminToggle.IsChecked = false;
                try
                {
                    Windows.Storage.ApplicationData.Current.LocalSettings.Values["AlwaysRunAsAdmin"] = false;
                }
                catch { }
            }
        }

        private void ShowAdminWarning_Click(object sender, RoutedEventArgs e)
        {
            bool showWarning = ShowAdminWarningToggle.IsChecked;
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                localSettings.Values["ShowAdminWarning"] = showWarning;
            }
            catch { }

            if (showWarning && !IsAdministrator())
            {
                AdminWarningBar.IsOpen = true;
            }
            else
            {
                AdminWarningBar.IsOpen = false;
            }
        }

        private void RestartAsAdminInfoBar_Click(object sender, RoutedEventArgs e)
        {
            RestartAsAdmin();
        }

        private void ViewHelp_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(HelpPage));
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        private void ResultsGrid_Sorting(object sender, CommunityToolkit.WinUI.UI.Controls.DataGridColumnEventArgs e)
        {
            string newSortColumn = e.Column.Tag?.ToString() ?? e.Column.Header.ToString();
            if (newSortColumn == _sortColumn)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = newSortColumn;
                _sortAscending = false;
            }
            
            foreach (var column in ResultsGrid.Columns)
            {
                if (column.Tag?.ToString() == _sortColumn || column.Header.ToString() == _sortColumn)
                {
                    column.SortDirection = _sortAscending 
                        ? CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Ascending 
                        : CommunityToolkit.WinUI.UI.Controls.DataGridSortDirection.Descending;
                }
                else
                {
                    column.SortDirection = null;
                }
            }

            SortData();
        }

        private void SortData()
        {
            if (_currentItems.Count == 0) return;

            string filter = FilterBox?.Text?.Trim()?.ToLowerInvariant() ?? "";
            var filtered = string.IsNullOrEmpty(filter) 
                ? _currentItems.ToList() 
                : _currentItems.Where(i => {
                    if (filter.StartsWith("*.")) return i.Name.ToLowerInvariant().EndsWith(filter.Substring(1));
                    return i.Name.ToLowerInvariant().Contains(filter);
                }).ToList();

            switch (_sortColumn)
            {
                case "Name":
                    filtered = _sortAscending ? filtered.OrderBy(x => x.Name).ToList() : filtered.OrderByDescending(x => x.Name).ToList();
                    break;
                case "Size":
                    filtered = _sortAscending ? filtered.OrderBy(x => x.SizeBytes).ToList() : filtered.OrderByDescending(x => x.SizeBytes).ToList();
                    break;
                case "Allocated":
                    filtered = _sortAscending ? filtered.OrderBy(x => x.AllocatedSizeBytes).ToList() : filtered.OrderByDescending(x => x.AllocatedSizeBytes).ToList();
                    break;
                case "Percentage":
                    filtered = _sortAscending ? filtered.OrderBy(x => x.ParentPercentage).ToList() : filtered.OrderByDescending(x => x.ParentPercentage).ToList();
                    break;
                case "Files":
                    filtered = _sortAscending ? filtered.OrderBy(x => x.FileCount).ToList() : filtered.OrderByDescending(x => x.FileCount).ToList();
                    break;
                case "Folders":
                    filtered = _sortAscending ? filtered.OrderBy(x => x.FolderCount).ToList() : filtered.OrderByDescending(x => x.FolderCount).ToList();
                    break;
                case "FreeSpace":
                    filtered = _sortAscending ? filtered.OrderBy(x => x.FreeSpaceBytes).ToList() : filtered.OrderByDescending(x => x.FreeSpaceBytes).ToList();
                    break;
                case "Modified":
                    filtered = _sortAscending ? filtered.OrderBy(x => x.LastModified).ToList() : filtered.OrderByDescending(x => x.LastModified).ToList();
                    break;
            }

            ResultsGrid.ItemsSource = new ObservableCollection<GridItemViewModel>(filtered);
            UpdateChart(filtered);
        }

        private void FilterBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            SortData();
        }

        private void ChartItemsControl_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            if (ResultsGrid.ItemsSource is ObservableCollection<GridItemViewModel> filtered)
            {
                UpdateChart(filtered.ToList());
            }
        }

        private void UpdateChart(System.Collections.Generic.List<GridItemViewModel> items)
        {
            if (ChartItemsControl == null) return;
            var chartItems = new ObservableCollection<ChartItemViewModel>();
            
            long totalSize = items.Sum(i => i.SizeBytes);
            if (totalSize == 0) {
                ChartItemsControl.ItemsSource = chartItems;
                return;
            }

            double maxHeight = 300; // max pixel height for the largest bar
            long maxSize = items.Max(i => i.SizeBytes);
            if (maxSize == 0) maxSize = 1;

            var colors = new[] { Microsoft.UI.Colors.CornflowerBlue, Microsoft.UI.Colors.SeaGreen, Microsoft.UI.Colors.IndianRed, Microsoft.UI.Colors.Goldenrod, Microsoft.UI.Colors.MediumPurple, Microsoft.UI.Colors.DarkOrange, Microsoft.UI.Colors.Teal, Microsoft.UI.Colors.Crimson };
            int colorIdx = 0;

            foreach (var item in items.OrderByDescending(i => i.SizeBytes).Take(15))
            {
                if (item.SizeBytes == 0) continue;
                double h = (item.SizeBytes * 1.0 / maxSize) * maxHeight;
                if (h < 5) h = 5;

                chartItems.Add(new ChartItemViewModel
                {
                    Name = item.Name,
                    Height = h,
                    Color = new Microsoft.UI.Xaml.Media.SolidColorBrush(colors[colorIdx % colors.Length]),
                    ToolTip = $"{item.Name} - {item.FormattedSize} ({item.FormattedPercentage})",
                    FormattedSize = item.FormattedSize
                });
                colorIdx++;
            }
            ChartItemsControl.ItemsSource = chartItems;
        }

        private async Task LoadDrivesAsync()
        {
            _currentPath = string.Empty;
            _pathSegments.Clear();
            _pathSegments.Add("This PC");
            BackButton.IsEnabled = false;

            var newItems = new List<GridItemViewModel>();
            var rootNodes = new ObservableCollection<FolderNode>();
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            
            foreach (var d in drives)
            {
                try
                {
                    var icon = await IconUtilities.GetIconAsync(d.Name, true);
                    
                    newItems.Add(new GridItemViewModel
                    {
                        Name = d.Name,
                        FullPath = d.Name,
                        IsFile = false,
                        SizeBytes = d.TotalSize - d.AvailableFreeSpace,
                        FormattedSize = DiskAnalyzerHelper.FormatSize(d.TotalSize - d.AvailableFreeSpace),
                        AllocatedSizeBytes = d.TotalSize - d.AvailableFreeSpace,
                        FormattedAllocated = DiskAnalyzerHelper.FormatSize(d.TotalSize - d.AvailableFreeSpace),
                        FileCount = 0,
                        FolderCount = 0,
                        ParentPercentage = d.TotalSize > 0 ? (double)(d.TotalSize - d.AvailableFreeSpace) / d.TotalSize * 100.0 : 0,
                        FreeSpaceBytes = d.AvailableFreeSpace,
                        FormattedFreeSpace = DiskAnalyzerHelper.FormatSize(d.AvailableFreeSpace),
                        LastModified = DateTime.MinValue,
                        IconSource = icon
                    });

                    var rootNode = new FolderNode 
                    { 
                        Name = d.Name, 
                        FullPath = d.Name, 
                        HasUnrealizedChildren = true,
                        IconSource = icon 
                    };
                    rootNodes.Add(rootNode);
                }
                catch
                {
                    // Ignore inaccessible or unreadable drives
                }
            }
            
            _currentItems = new ObservableCollection<GridItemViewModel>(newItems);
            ResultsGrid.ItemsSource = _currentItems;
            
            FolderTree.ItemsSource = rootNodes;
            
            SortData();
        }



        private void SetLoading(bool isLoading)
        {
            ScanProgressBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            ResultsGrid.Opacity = isLoading ? 0.5 : 1.0;
        }

        
        private async Task SyncTreeViewToPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string driveRoot = parts[0] + Path.DirectorySeparatorChar;
            var rootNodes = FolderTree.ItemsSource as System.Collections.ObjectModel.ObservableCollection<FolderNode>;
            if (rootNodes == null) return;

            var currentLevel = rootNodes;
            FolderNode? targetNode = null;

            string currentPath = driveRoot;
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0)
                {
                    currentPath = Path.Combine(currentPath, parts[i]);
                }

                var node = currentLevel.FirstOrDefault(n => n.FullPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase));
                if (node == null) break;

                targetNode = node;

                if (i < parts.Length - 1)
                {
                    if (node.HasUnrealizedChildren)
                    {
                        var subDirs = await Task.Run(() => 
                        {
                            try { return new DirectoryInfo(node.FullPath).GetDirectories(); } 
                            catch { return Array.Empty<DirectoryInfo>(); }
                        });
                        
                        node.Children.Clear();
                        foreach(var d in subDirs)
                        {
                            node.Children.Add(new FolderNode { Name = d.Name, FullPath = d.FullName, HasUnrealizedChildren = true });
                        }
                        node.HasUnrealizedChildren = false;
                    }
                    currentLevel = node.Children;
                    
                    // Tell TreeView to expand this node
                    var container = FolderTree.ContainerFromItem(node) as Microsoft.UI.Xaml.Controls.TreeViewItem;
                    if (container != null)
                    {
                        container.IsExpanded = true;
                    }
                }
            }

            if (targetNode != null)
            {
                FolderTree.SelectedItem = targetNode;
            }
        }
private async Task NavigateToFolderAsync(string path)
        {
            _navigationCts?.Cancel();
            _navigationCts = new System.Threading.CancellationTokenSource();
            var token = _navigationCts.Token;

            _currentPath = path;
            
            _pathSegments.Clear();
            _pathSegments.Add("This PC");
            
            bool isUnc = path.StartsWith(@"\\");
            var parts = path.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            if (isUnc && parts.Length >= 2)
            {
                _pathSegments.Add(@"\\" + parts[0] + @"\" + parts[1]);
                for (int i = 2; i < parts.Length; i++) _pathSegments.Add(parts[i]);
            }
            else
            {
                foreach (var p in parts) _pathSegments.Add(p);
            }
            
            BackButton.IsEnabled = true;
            _currentItems.Clear();
            
            SetLoading(true);
            try
            {
                // Stream results to the UI as they are scanned
                ResultsGrid.ItemsSource = _currentItems;
                var progress = new Progress<DiskItemInfo>(async item => 
                {
                    if (token.IsCancellationRequested) return;

                    var vm = new GridItemViewModel
                    {
                        Name = item.Name,
                        FullPath = item.FullPath,
                        FormattedSize = DiskAnalyzerHelper.FormatSize(item.SizeBytes),
                        FormattedAllocated = DiskAnalyzerHelper.FormatSize(item.AllocatedSizeBytes),
                        FileCount = item.FileCount,
                        FolderCount = item.FolderCount,
                        ParentPercentage = 0, // Calculated after scan finishes
                        FreeSpaceBytes = 0,
                        FormattedFreeSpace = "",
                        LastModified = item.LastModified,
                        SizeBytes = item.SizeBytes,
                        AllocatedSizeBytes = item.AllocatedSizeBytes,
                        IsFile = item.IsFile,
                        IconSource = null
                    };

                    _currentItems.Add(vm);
                    
                    if (!item.IsFile)
                    {
                        try { vm.IconSource = await IconUtilities.GetIconAsync(item.FullPath, true); } catch { }
                    }
                });

                var items = await Task.Run(() => DiskAnalyzerHelper.ScanDirectory(path, 1, _showHiddenFiles, progress), token);
                if (token.IsCancellationRequested) return;

                long parentSize = items.Sum(i => i.SizeBytes);

                // Update percentages after all items have been scanned
                foreach (var vm in _currentItems.ToList())
                {
                    vm.ParentPercentage = parentSize > 0 ? (vm.SizeBytes * 100.0 / parentSize) : 0;
                }
                
                SortData();
            }
            catch (Exception)
            {
                // Handle unreadable folders
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    SetLoading(false);
                }
            }
        }

        private async void ResultsGrid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is GridItemViewModel item && !item.IsFile)
            {
                await NavigateToFolderAsync(item.FullPath);
            }
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPath)) return;

            var parent = Directory.GetParent(_currentPath);
            if (parent != null)
            {
                await NavigateToFolderAsync(parent.FullName);
            }
            else
            {
                _ = LoadDrivesAsync();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                await LoadDrivesAsync();
            }
            else
            {
                await NavigateToFolderAsync(_currentPath);
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add("*");

            // Required for WinUI 3 desktop apps
            var hwnd = WindowNative.GetWindowHandle((Application.Current as App)?.MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await NavigateToFolderAsync(folder.Path);
            }
        }

        private void ThemeMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is string tag)
            {
                var app = Application.Current as App;
                if (app?.MainWindow?.Content is FrameworkElement frameworkElement)
                {
                    if (tag == "Light")
                        frameworkElement.RequestedTheme = ElementTheme.Light;
                    else if (tag == "Dark")
                        frameworkElement.RequestedTheme = ElementTheme.Dark;
                    else
                        frameworkElement.RequestedTheme = ElementTheme.Default;

                    // Save theme preference
                    try
                    {
                        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                        localSettings.Values["Theme"] = tag;
                    }
                    catch { }
                }
            }
        }

        private void ExitMenu_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void ShowHiddenFiles_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            _showHiddenFiles = ShowHiddenFilesToggle.IsChecked;
            RefreshButton_Click(this, new RoutedEventArgs());
        }

        private void ExpandAll_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (FolderTree.ItemsSource is IEnumerable<FolderNode> rootNodes)
            {
                foreach (var node in rootNodes)
                {
                    var container = FolderTree.ContainerFromItem(node);
                    if (container != null)
                    {
                        var treeNode = FolderTree.NodeFromContainer(container);
                        if (treeNode != null) treeNode.IsExpanded = true;
                    }
                }
            }
        }

        private void CollapseAll_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (FolderTree.ItemsSource is IEnumerable<FolderNode> rootNodes)
            {
                foreach (var node in rootNodes)
                {
                    var container = FolderTree.ContainerFromItem(node);
                    if (container != null)
                    {
                        var treeNode = FolderTree.NodeFromContainer(container);
                        if (treeNode != null) treeNode.IsExpanded = false;
                    }
                }
            }
        }

        private async void PathBreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
        {
            if (args.Index == 0)
            {
                await LoadDrivesAsync();
                return;
            }

            string path = string.Join(Path.DirectorySeparatorChar.ToString(), _pathSegments.Skip(1).Take(args.Index));
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()) && !path.Contains(Path.DirectorySeparatorChar.ToString()))
            {
                path += Path.DirectorySeparatorChar; // C: -> C:\
            }
            
            await NavigateToFolderAsync(path);
        }

        private async void FolderTree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
        {
            if (args.Item is FolderNode node && node.HasUnrealizedChildren)
            {
                node.HasUnrealizedChildren = false; // Prevent race conditions on double clicks
                node.Children.Clear();
                try
                {
                    var dirInfo = new DirectoryInfo(node.FullPath);
                    var directories = await Task.Run(() => dirInfo.EnumerateDirectories()
                        .Where(d => (d.Attributes & FileAttributes.ReparsePoint) == 0)
                        .OrderBy(d => d.Name)
                        .ToList());

                    foreach (var d in directories)
                    {
                        var childNode = new FolderNode 
                        { 
                            Name = d.Name, 
                            FullPath = d.FullName, 
                            HasUnrealizedChildren = true,
                            IconSource = await IconUtilities.GetIconAsync(d.FullName, true)
                        };
                        node.Children.Add(childNode);
                    }
                }
                catch { }
            }
        }

        private async void FolderTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is FolderNode node && !string.IsNullOrEmpty(node.FullPath))
            {
                await NavigateToFolderAsync(node.FullPath);
            }
        }
    }

    public class FolderNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool HasUnrealizedChildren { get; set; }
        public Microsoft.UI.Xaml.Media.ImageSource? IconSource { get; set; }
        public ObservableCollection<FolderNode> Children { get; set; } = new ObservableCollection<FolderNode>();
    }

    public class ChartItemViewModel
    {
        public string Name { get; set; } = "";
        public double Height { get; set; }
        public Microsoft.UI.Xaml.Media.SolidColorBrush Color { get; set; } = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
        public string ToolTip { get; set; } = "";
        public string FormattedSize { get; set; } = "";
    }

    public class GridItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string FormattedSize { get; set; } = "";
        public string FormattedAllocated { get; set; } = "";
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
        private double _parentPercentage;
        public double ParentPercentage
        {
            get => _parentPercentage;
            set { _parentPercentage = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedPercentage)); }
        }
        public string FormattedPercentage => $"{ParentPercentage:F1}%";
        public string FormattedFreeSpace { get; set; } = "";
        public long FreeSpaceBytes { get; set; }
        public DateTime LastModified { get; set; }
        /// <summary>Returns a formatted date string, or empty for drives (DateTime.MinValue).</summary>
        public string FormattedDate =>
            LastModified == DateTime.MinValue ? "" : LastModified.ToString("M/d/yyyy h:mm:ss tt");
        public long SizeBytes { get; set; }
        public long AllocatedSizeBytes { get; set; }
        public bool IsFile { get; set; }
        public Microsoft.UI.Xaml.Media.ImageSource? IconSource { get; set; }
    }
}
