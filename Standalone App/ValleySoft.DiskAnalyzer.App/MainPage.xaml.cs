using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using Community.PowerToys.Run.Plugin.DiskAnalyzer;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;
using Windows.UI;
using Microsoft.VisualBasic.FileIO;

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
        private ObservableCollection<TopFileResult> _topFiles = new ObservableCollection<TopFileResult>();
        private bool _topFilesScanned = false;
        private DispatcherTimer? _filterDebounceTimer;

        public MainPage()
        {
            try
            {
                this.InitializeComponent();
                ResultsGrid.ItemsSource = _currentItems;
                TopFilesGrid.ItemsSource = _topFiles;
                PathBreadcrumbBar.ItemsSource = _pathSegments;
                _currentItems.CollectionChanged += (s, e) => UpdateItemCount();
            }
            catch (Exception ex)
            {
                App.WriteCrashLog(ex);
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

                if (IsWindowsInsiderBuild())
                {
                    InsiderWarningBar.IsOpen = true;
                }
            }
            catch { }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string path && !string.IsNullOrWhiteSpace(path))
            {
                await NavigateToFolderAsync(path);
            }
            else
            {
                await LoadDrivesAsync();
            }
        }

        private static bool IsWindowsInsiderBuild()
        {
            try
            {
                // Check if the machine is actively enrolled in the Windows Insider flighting rings
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\WindowsSelfHost\Applicability"))
                {
                    if (key != null)
                    {
                        var ring = key.GetValue("Ring") as string;
                        var branch = key.GetValue("BranchName") as string;
                        if (!string.IsNullOrEmpty(ring) || !string.IsNullOrEmpty(branch))
                        {
                            return true;
                        }
                    }
                }

                // Fallback: If no active registry enrollment, check if build number exceeds the current stable release version (25H2 is build 26200)
                if (Environment.OSVersion.Version.Build > 26300)
                {
                    return true;
                }
            }
            catch { }
            return false;
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
            try
            {
                // MSIX apps must use the App Execution Alias for elevation.
                // MainModule.FileName points to C:\Program Files\WindowsApps\ which is
                // protected and cannot be ShellExecuted with 'runas' from a packaged context.
                // The alias in %LOCALAPPDATA%\Microsoft\WindowsApps\ is the correct target.
                string aliasPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "WindowsApps", "ValleySoft.DiskAnalyzer.exe");

                bool aliasExists = false;
                try
                {
                    var attr = System.IO.File.GetAttributes(aliasPath);
                    if (attr != (System.IO.FileAttributes)(-1))
                    {
                        aliasExists = true;
                    }
                }
                catch { }

                string exePath = aliasExists ? aliasPath
                    : System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                      ?? "ValleySoft.DiskAnalyzer.exe";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exePath,
                    Verb = "runas",           // Triggers UAC elevation prompt
                    UseShellExecute = true,   // Required for Verb to work
                    CreateNoWindow = false
                };

                // Start elevated instance first — if UAC is cancelled this throws, so we don't exit
                System.Diagnostics.Process.Start(startInfo);

                // Only exit the current (non-admin) instance after the elevated one launches
                Application.Current.Exit();
            }
            catch
            {
                // User cancelled UAC prompt — stay in current instance, revert toggles
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
            try
            {
                if (this.Frame != null)
                {
                    this.Frame.Navigate(typeof(HelpPage));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ViewHelp_Click error: {ex.Message}");
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.Frame != null)
                {
                    this.Frame.Navigate(typeof(AboutPage));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"About_Click error: {ex.Message}");
            }
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

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _filterDebounceTimer?.Stop();
            _filterDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _filterDebounceTimer.Tick += (s, args) =>
            {
                _filterDebounceTimer.Stop();
                SortData();
            };
            _filterDebounceTimer.Start();
        }

        private void SortData()
        {
            if (_currentItems == null || _currentItems.Count == 0) return;

            string filter = FilterBox?.Text?.Trim()?.ToLowerInvariant() ?? "";
            bool isLargeOld = LargeOldFilterButton?.IsChecked == true;

            var filtered = _currentItems.Where(i => {
                if (isLargeOld)
                {
                    // Files: must be >= 50 MB AND modified older than 6 months
                    // Folders: keep large folders (>= 100 MB) visible so users can drill in to find large contents
                    if (i.IsFile)
                    {
                        if (i.SizeBytes < 50 * 1024 * 1024 || i.LastModified > DateTime.Now.AddMonths(-6))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // Folder: keep if large (>= 100 MB) or still scanning (size == 0)
                        if (i.SizeBytes > 0 && i.SizeBytes < 100 * 1024 * 1024)
                        {
                            return false;
                        }
                    }
                }

                if (string.IsNullOrEmpty(filter)) return true;

                string nameLower = (i.Name ?? "").ToLowerInvariant();
                if (filter.StartsWith("*."))
                {
                    string ext = filter.Substring(1);
                    return nameLower.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
                }
                if (filter.StartsWith("."))
                {
                    return nameLower.EndsWith(filter, StringComparison.OrdinalIgnoreCase);
                }
                return nameLower.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                       nameLower.EndsWith("." + filter, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            if (LargeOldFilterText != null)
            {
                if (isLargeOld)
                {
                    int matchCount = filtered.Count;
                    LargeOldFilterText.Text = $"🕐 Old & Large ({matchCount})";
                }
                else
                {
                    LargeOldFilterText.Text = "🕐 Old & Large";
                }
            }

            // Update item count with filter indicator
            if (ItemCountText != null)
            {
                if (filtered.Count != _currentItems.Count)
                {
                    ItemCountText.Text = $"{filtered.Count} of {_currentItems.Count} items (filtered)";
                }
                else
                {
                    ItemCountText.Text = _currentItems.Count == 1 ? "1 item" : $"{_currentItems.Count} items";
                }
            }

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

            // Only update heavy chart geometry if the respective chart tab is actually visible
            string? activeTab = (MainPivot?.SelectedItem as PivotItem)?.Header?.ToString();
            if (activeTab == "Visual Chart")
            {
                UpdateChart(filtered);
            }
            else if (activeTab == "Donut Chart")
            {
                RefreshDonutChart(filtered);
            }
            CalculateFileTypeBreakdown();
        }

        private void CalculateFileTypeBreakdown()
        {
            if (string.IsNullOrEmpty(_currentPath))
            {
                TypeBreakdownList.ItemsSource = null;
                return;
            }

            string path = _currentPath;
            bool includeHidden = _showHiddenFiles;

            // Start dynamic background calculation to prevent blocking WinUI UI thread
            Task.Run(() =>
            {
                try
                {
                    var breakdown = DiskAnalyzerHelper.GetFileTypeBreakdown(path, includeHidden);
                    var colorMap = new Dictionary<string, Color>
                    {
                        { "Videos", Microsoft.UI.ColorHelper.FromArgb(255, 107, 102, 255) },       // Vibrant Violet/Indigo
                        { "Audio", Microsoft.UI.ColorHelper.FromArgb(255, 255, 69, 58) },         // Vibrant Coral Red
                        { "Images", Microsoft.UI.ColorHelper.FromArgb(255, 255, 159, 10) },       // Vibrant Amber Orange
                        { "Archives", Microsoft.UI.ColorHelper.FromArgb(255, 255, 214, 10) },     // Vibrant Bright Gold
                        { "Documents", Microsoft.UI.ColorHelper.FromArgb(255, 191, 90, 242) },    // Vibrant Purple
                        { "Code", Microsoft.UI.ColorHelper.FromArgb(255, 48, 209, 88) },          // Vibrant Mint Green
                        { "Apps/Executables", Microsoft.UI.ColorHelper.FromArgb(255, 100, 210, 255) }, // Vibrant Electric Cyan
                        { "Other Files", Microsoft.UI.ColorHelper.FromArgb(255, 174, 174, 178) }  // Light Silver Gray
                    };

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_currentPath == path)
                        {
                            var list = new List<TypeCategoryViewModel>();
                            foreach (var entry in breakdown)
                            {
                                var color = colorMap.ContainsKey(entry.Category) ? colorMap[entry.Category] : Microsoft.UI.ColorHelper.FromArgb(255, 174, 174, 178);
                                list.Add(new TypeCategoryViewModel
                                {
                                    Name = entry.Category,
                                    SizeBytes = entry.Size,
                                    FormattedSize = DiskAnalyzerHelper.FormatSize(entry.Size),
                                    Percentage = entry.Percentage,
                                    ColorBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(color) // Safe: Created on the UI thread
                                });
                            }
                            TypeBreakdownList.ItemsSource = list;
                        }
                    });
                }
                catch (Exception ex)
                {
                    App.WriteCrashLog(ex);
                }
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    App.WriteCrashLog(t.Exception?.InnerException ?? t.Exception!);
                }
            }, TaskScheduler.Default);
        }

        private void ChartItemsControl_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            if (ResultsGrid.ItemsSource is ObservableCollection<GridItemViewModel> filtered)
            {
                UpdateChart(filtered.ToList());
            }
        }

        private void UpdateChart(List<GridItemViewModel> items)
        {
            if (items == null || items.Count == 0)
            {
                ChartItemsControl.ItemsSource = null;
                return;
            }

            double availableHeight = MainPivot.ActualHeight;
            double maxHeight = availableHeight > 150 ? availableHeight - 150 : 160;
            if (maxHeight < 100) maxHeight = 100;

            double maxSize = items.Max(i => i.SizeBytes);
            if (maxSize <= 0) maxSize = 1;

            var chartItems = new List<ChartItemViewModel>();
            var colors = new[]
            {
                Microsoft.UI.ColorHelper.FromArgb(255, 100, 210, 255),
                Microsoft.UI.ColorHelper.FromArgb(255, 48, 209, 88),
                Microsoft.UI.ColorHelper.FromArgb(255, 255, 159, 10),
                Microsoft.UI.ColorHelper.FromArgb(255, 255, 69, 58),
                Microsoft.UI.ColorHelper.FromArgb(255, 191, 90, 242),
                Microsoft.UI.ColorHelper.FromArgb(255, 255, 214, 10),
                Microsoft.UI.ColorHelper.FromArgb(255, 107, 102, 255),
                Microsoft.UI.ColorHelper.FromArgb(255, 174, 174, 178)
            };
            int colorIdx = 0;

            var sortedItems = items.OrderByDescending(i => i.SizeBytes).ToList();
            int topLimit = 15;
            var topItems = sortedItems.Take(topLimit).ToList();
            var remainingItems = sortedItems.Skip(topLimit).ToList();

            foreach (var item in topItems)
            {
                if (item.SizeBytes == 0) continue;
                double h = (item.SizeBytes * 1.0 / maxSize) * maxHeight;
                if (h < 5) h = 5;

                chartItems.Add(new ChartItemViewModel
                {
                    Name = item.Name,
                    FullPath = item.FullPath,
                    IsFile = item.IsFile,
                    Height = h,
                    Color = new Microsoft.UI.Xaml.Media.SolidColorBrush(colors[colorIdx % colors.Length]),
                    ToolTip = $"{item.Name} - {item.FormattedSize} ({item.FormattedPercentage})",
                    FormattedSize = item.FormattedSize
                });
                colorIdx++;
            }

            // Aggregate remaining items into an "Other Items" summary bar
            if (remainingItems.Count > 0)
            {
                long otherSizeBytes = remainingItems.Sum(i => i.SizeBytes);
                if (otherSizeBytes > 0)
                {
                    double otherHeight = (otherSizeBytes * 1.0 / maxSize) * maxHeight;
                    if (otherHeight < 5) otherHeight = 5;
                    string formattedOtherSize = DiskAnalyzerHelper.FormatSize(otherSizeBytes);

                    chartItems.Add(new ChartItemViewModel
                    {
                        Name = $"Other ({remainingItems.Count} items)",
                        FullPath = "",
                        IsFile = false,
                        Height = otherHeight,
                        Color = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 120, 120, 128)),
                        ToolTip = $"Other {remainingItems.Count} items - {formattedOtherSize}",
                        FormattedSize = formattedOtherSize
                    });
                }
            }

            ChartItemsControl.ItemsSource = chartItems;
        }

        private async void ChartItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.DataContext is ChartItemViewModel vm)
            {
                if (vm.Name.StartsWith("Other")) return;

                if (!string.IsNullOrEmpty(vm.FullPath))
                {
                    if (!vm.IsFile)
                    {
                        await NavigateToFolderAsync(vm.FullPath);
                    }
                    else
                    {
                        try
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{vm.FullPath}\"");
                        }
                        catch { }
                    }
                }
            }
        }

        private async Task LoadDrivesAsync()
        {
            _currentPath = string.Empty;
            _topFilesScanned = false;
            _pathSegments.Clear();
            _pathSegments.Add("This PC");
            BackButton.IsEnabled = false;

            var newItems = new List<GridItemViewModel>();
            var rootNodes = new ObservableCollection<FolderNode>();
            
            var driveData = await Task.Run(() => 
            {
                var data = new List<(string Name, long TotalSize, long AvailableFreeSpace)>();
                foreach (var d in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (d.IsReady)
                        {
                            data.Add((d.Name, d.TotalSize, d.AvailableFreeSpace));
                        }
                    }
                    catch { } // Ignore unready or inaccessible
                }
                return data;
            });
            
            foreach (var d in driveData)
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
            _currentItems.CollectionChanged += (s, e) => UpdateItemCount();
            ResultsGrid.ItemsSource = _currentItems;
            UpdateItemCount();
            
            FolderTree.ItemsSource = rootNodes;
            
            SortData();
        }



        private void SetLoading(bool isLoading)
        {
            ScanProgressBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            ResultsGrid.Opacity = isLoading ? 0.5 : 1.0;
            if (ExportCsvButton != null)
            {
                ExportCsvButton.IsEnabled = !isLoading && !string.IsNullOrWhiteSpace(_currentPath) && (_currentItems != null && _currentItems.Count > 0);
            }
        }

        private void UpdateItemCount()
        {
            // CollectionChanged fires on the UI thread for our usage, but guard just in case
            var count = _currentItems.Count;
            var text = count == 1 ? "1 item" : $"{count} items";
            if (ItemCountText != null)
                ItemCountText.Text = text;

            if (ExportCsvButton != null && ScanProgressBar.Visibility != Visibility.Visible)
            {
                ExportCsvButton.IsEnabled = !string.IsNullOrWhiteSpace(_currentPath) && count > 0;
            }
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
            _navigationCts?.Dispose();
            _navigationCts = new System.Threading.CancellationTokenSource();
            var token = _navigationCts.Token;

            _currentPath = path;
            _topFilesScanned = false;
            
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
                var progress = new Progress<DiskItemInfo>(item => 
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
                });

                var items = await Task.Run(() => DiskAnalyzerHelper.ScanDirectory(path, 1, _showHiddenFiles, progress), token);
                if (token.IsCancellationRequested) return;

                // Reconcile: folders arrived via progress (live streaming), files are only in
                // the returned list. Add any items not yet shown in the grid.
                var reportedPaths = new HashSet<string>(
                    _currentItems.Select(vm => vm.FullPath), StringComparer.OrdinalIgnoreCase);
                foreach (var item in items)
                {
                    if (reportedPaths.Contains(item.FullPath)) continue;
                    if (token.IsCancellationRequested) break;
                    _currentItems.Add(new GridItemViewModel
                    {
                        Name = item.Name,
                        FullPath = item.FullPath,
                        FormattedSize = DiskAnalyzerHelper.FormatSize(item.SizeBytes),
                        FormattedAllocated = DiskAnalyzerHelper.FormatSize(item.AllocatedSizeBytes),
                        FileCount = item.FileCount,
                        FolderCount = item.FolderCount,
                        ParentPercentage = 0,
                        FreeSpaceBytes = 0,
                        FormattedFreeSpace = "",
                        LastModified = item.LastModified,
                        SizeBytes = item.SizeBytes,
                        AllocatedSizeBytes = item.AllocatedSizeBytes,
                        IsFile = item.IsFile,
                        IconSource = null
                    });
                }

                long parentSize = items.Sum(i => i.SizeBytes);

                // Update percentages after all items have been scanned
                foreach (var vm in _currentItems.ToList())
                {
                    vm.ParentPercentage = parentSize > 0 ? (vm.SizeBytes * 100.0 / parentSize) : 0;
                }
                
                SortData();

                // Load icons asynchronously in background after initial grid render.
                // IMPORTANT: snapshot must be taken HERE on the UI thread — calling
                // _currentItems.ToList() from inside Task.Run causes a race condition with
                // the UI thread and triggers FATAL_USER_CALLBACK_EXCEPTION in Composition.
                var iconSnapshot = _currentItems.ToList();
                _ = Task.Run(async () =>
                {
                    foreach (var vm in iconSnapshot)
                    {
                        if (token.IsCancellationRequested) break;
                        try
                        {
                            var icon = await IconUtilities.GetIconAsync(vm.FullPath, !vm.IsFile, DispatcherQueue);
                            if (icon != null)
                            {
                                DispatcherQueue.TryEnqueue(() => vm.IconSource = icon);
                            }
                        }
                        catch { }
                    }
                });
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
                    CheckAndShowRatingPromptAsync();
                }
            }
        }

        private async void CheckAndShowRatingPromptAsync()
        {
            try
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                bool neverShow = localSettings.Values["NeverShowRatingPrompt"] as bool? ?? false;
                if (neverShow) return;

                int scanCount = localSettings.Values["CompletedScanCount"] as int? ?? 0;
                scanCount++;
                localSettings.Values["CompletedScanCount"] = scanCount;

                // Show only once, after the user has completed 10 scans —
                // they're a genuine regular user at that point, not a first-timer.
                if (scanCount != 10) return;

                // Wait 5 seconds so results are fully visible before the dialog appears.
                await Task.Delay(TimeSpan.FromSeconds(5));

                // Check again in case the user navigated away or started another scan.
                neverShow = localSettings.Values["NeverShowRatingPrompt"] as bool? ?? false;
                if (neverShow) return;

                var promptContent = new StackPanel { Spacing = 12 };
                promptContent.Children.Add(new TextBlock
                {
                    Text = "You've been using Disk Analyzer for a while — thank you! If it's been useful, a quick rating on the Microsoft Store helps a lot.",
                    TextWrapping = TextWrapping.Wrap
                });

                var ratingDialog = new ContentDialog
                {
                    Title = "Enjoying Disk Analyzer?",
                    Content = promptContent,
                    PrimaryButtonText = "Leave a Rating",
                    SecondaryButtonText = "Not Now",
                    CloseButtonText = "Don't Ask Again",
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.XamlRoot
                };

                // Mark as shown regardless of outcome — we only ever ask once.
                localSettings.Values["NeverShowRatingPrompt"] = true;

                var result = await ratingDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9NF073KLTVWN"));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Rating prompt error: {ex.Message}");
            }
        }



        private async void ResultsGrid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is GridItemViewModel item && !item.IsFile)
            {
                await NavigateToFolderAsync(item.FullPath);
            }
        }

        // --- Touchpad / touchscreen scroll support for DataGrid ---
        // The CommunityToolkit DataGrid swallows PointerWheelChanged and ManipulationDelta
        // events without forwarding them to its internal ScrollViewer, breaking touchpad
        // precision scrolling and touchscreen pan gestures. We intercept both and
        // programmatically drive the inner ScrollViewer instead.

        private ScrollViewer? _dataGridScrollViewer;

        private ScrollViewer? GetDataGridScrollViewer()
        {
            if (_dataGridScrollViewer != null) return _dataGridScrollViewer;
            _dataGridScrollViewer = FindChildScrollViewer(ResultsGrid);
            return _dataGridScrollViewer;
        }

        private static ScrollViewer? FindChildScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer sv) return sv;
                var found = FindChildScrollViewer(child);
                if (found != null) return found;
            }
            return null;
        }

        private void ResultsGrid_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var sv = GetDataGridScrollViewer();
            if (sv == null) return;

            var props = e.GetCurrentPoint(ResultsGrid).Properties;
            // Vertical scroll: positive delta = scroll up, negative = scroll down
            double scrollAmount = -props.MouseWheelDelta / 3.0;
            sv.ChangeView(null, sv.VerticalOffset + scrollAmount, null, false);
            e.Handled = true;
        }

        private void ResultsGrid_ManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
        {
            var sv = GetDataGridScrollViewer();
            if (sv == null) return;

            // Translate touch pan gesture into vertical scroll (invert Y for natural scrolling)
            sv.ChangeView(null, sv.VerticalOffset - e.Delta.Translation.Y, null, false);
            e.Handled = true;
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

        private IntPtr GetWindowHandle()
        {
            try
            {
                var appWindow = (Application.Current as App)?.MainWindow;
                if (appWindow != null)
                {
                    var hwnd = WindowNative.GetWindowHandle(appWindow);
                    if (hwnd != IntPtr.Zero) return hwnd;
                }
            }
            catch { }
            try
            {
                return System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            }
            catch { }
            return IntPtr.Zero;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct OPENFILENAME
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetSaveFileName([In, Out] ref OPENFILENAME ofn);

        private string? ShowNativeSaveFileDialog(string defaultFileName)
        {
            try
            {
                var ofn = new OPENFILENAME();
                ofn.lStructSize = Marshal.SizeOf(ofn);
                ofn.hwndOwner = GetWindowHandle();
                ofn.lpstrFilter = "CSV File (*.csv)\0*.csv\0All Files (*.*)\0*.*\0\0";
                ofn.lpstrFile = defaultFileName.PadRight(260, '\0');
                ofn.nMaxFile = 260;
                ofn.lpstrDefExt = "csv";
                ofn.Flags = 0x00080000 | 0x00000002 | 0x00000004; // OFN_EXPLORER | OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT

                if (GetSaveFileName(ref ofn))
                {
                    return ofn.lpstrFile.TrimEnd('\0');
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowNativeSaveFileDialog: {ex.Message}");
            }
            return null;
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add("*");

                var hwnd = GetWindowHandle();
                if (hwnd != IntPtr.Zero)
                {
                    InitializeWithWindow.Initialize(folderPicker, hwnd);
                }

                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    await NavigateToFolderAsync(folder.Path);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in BrowseButton_Click: {ex.Message}");
            }
        }

        private async void ExportToCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string activeTab = (MainPivot?.SelectedItem as PivotItem)?.Header?.ToString() ?? "";
                bool isTopFilesTab = activeTab == "Top Files";

                if (isTopFilesTab)
                {
                    if (_topFiles == null || _topFiles.Count == 0)
                    {
                        var noItemsDialog = new ContentDialog
                        {
                            Title = "Export CSV",
                            Content = "No top files have been scanned yet to export.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await noItemsDialog.ShowAsync();
                        return;
                    }
                }
                else
                {
                    if (_currentItems == null || _currentItems.Count == 0)
                    {
                        var noItemsDialog = new ContentDialog
                        {
                            Title = "Export CSV",
                            Content = "No data is currently loaded to export.",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await noItemsDialog.ShowAsync();
                        return;
                    }
                }

                string safeFolderName = string.Join("_", _currentPath.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Replace(" ", "_").Trim();
                if (string.IsNullOrWhiteSpace(safeFolderName)) safeFolderName = "DiskAnalysis";
                if (isTopFilesTab) safeFolderName += "_TopFiles";
                string defaultFileName = $"{safeFolderName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                string? destinationPath = null;

                // 1. Try WinRT FileSavePicker first
                try
                {
                    var savePicker = new FileSavePicker();
                    var hwnd = GetWindowHandle();
                    if (hwnd != IntPtr.Zero)
                    {
                        InitializeWithWindow.Initialize(savePicker, hwnd);
                    }
                    savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                    savePicker.FileTypeChoices.Add("CSV File (*.csv)", new List<string>() { ".csv" });
                    savePicker.SuggestedFileName = defaultFileName;

                    var file = await savePicker.PickSaveFileAsync();
                    if (file != null)
                    {
                        destinationPath = file.Path;
                    }
                }
                catch { }

                // 2. If WinRT FileSavePicker failed or returned null (e.g. running elevated as Admin), use native comdlg32 fallback
                if (string.IsNullOrEmpty(destinationPath))
                {
                    destinationPath = ShowNativeSaveFileDialog(defaultFileName);
                }

                if (string.IsNullOrEmpty(destinationPath))
                {
                    return; // User explicitly cancelled or closed the dialog
                }

                var sb = new System.Text.StringBuilder();
                int exportedCount = 0;

                if (isTopFilesTab)
                {
                    sb.AppendLine("Name,Path,Size (Bytes),Size,Last Modified");
                    var topFilesList = _topFiles.ToList();
                    exportedCount = topFilesList.Count;
                    foreach (var item in topFilesList)
                    {
                        string name = (item.Name ?? "").Replace("\"", "\"\"");
                        string path = (item.FullPath ?? "").Replace("\"", "\"\"");
                        long sizeBytes = item.SizeBytes;
                        string formattedSize = (item.FormattedSize ?? "").Replace("\"", "\"\"");
                        string modified = item.LastModified == DateTime.MinValue ? "" : item.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
                        sb.AppendLine($"\"{name}\",\"{path}\",{sizeBytes},\"{formattedSize}\",\"{modified}\"");
                    }
                }
                else
                {
                    var itemsToExport = (ResultsGrid.ItemsSource as IEnumerable<GridItemViewModel>)?.ToList() ?? _currentItems.ToList();
                    exportedCount = itemsToExport.Count;
                    sb.AppendLine("Name,Path,Type,Size (Bytes),Size,Allocated Size (Bytes),Allocated Size,% of Parent,File Count,Folder Count,Last Modified");
                    foreach (var item in itemsToExport)
                    {
                        string name = (item.Name ?? "").Replace("\"", "\"\"");
                        string path = (item.FullPath ?? "").Replace("\"", "\"\"");
                        string type = item.IsFile ? "File" : "Directory";
                        long sizeBytes = item.SizeBytes;
                        string formattedSize = (item.FormattedSize ?? "").Replace("\"", "\"\"");
                        long allocatedSizeBytes = item.AllocatedSizeBytes;
                        string formattedAllocated = (item.FormattedAllocated ?? "").Replace("\"", "\"\"");
                        string pct = (item.FormattedPercentage ?? "0%").Replace("\"", "\"\"");
                        string files = item.IsFile ? "0" : item.FileCount.ToString();
                        string folders = item.IsFile ? "0" : item.FolderCount.ToString();
                        string modified = item.LastModified == DateTime.MinValue ? "" : item.LastModified.ToString("yyyy-MM-dd HH:mm:ss");
                        sb.AppendLine($"\"{name}\",\"{path}\",\"{type}\",{sizeBytes},\"{formattedSize}\",{allocatedSizeBytes},\"{formattedAllocated}\",\"{pct}\",{files},{folders},\"{modified}\"");
                    }
                }

                System.IO.File.WriteAllText(destinationPath, sb.ToString(), new System.Text.UTF8Encoding(true));

                var successDialog = new ContentDialog
                {
                    Title = "Export Complete",
                    Content = $"Successfully exported {exportedCount} items to:\n{destinationPath}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                string errorDetails = !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : $"{ex.GetType().Name} ({ex.HResult:X8})";
                var errorDialog = new ContentDialog
                {
                    Title = "Export Failed",
                    Content = $"An error occurred while exporting CSV: {errorDetails}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }





        private void ThemeMenuItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is string tag)
            {
                var app = Application.Current as App;
                app?.MainWindow?.SetAppTheme(tag);

                // Save theme preference
                try
                {
                    var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                    localSettings.Values["Theme"] = tag;
                }
                catch { }
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

        private void DataGrid_OpenInExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is GridItemViewModel vm)
            {
                try
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{vm.FullPath}\"");
                }
                catch { }
            }
        }

        private void DataGrid_CopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is GridItemViewModel vm)
            {
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(vm.FullPath);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
            }
        }

        private void BreadcrumbContainer_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            PathBreadcrumbBar.Visibility = Visibility.Collapsed;
            EditablePathBox.Visibility = Visibility.Visible;
            EditablePathBox.Text = _currentPath;
            EditablePathBox.Focus(FocusState.Programmatic);
            EditablePathBox.SelectAll();
        }

        private async void EditablePathBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                string path = EditablePathBox.Text;
                if (Directory.Exists(path))
                {
                    PathBreadcrumbBar.Visibility = Visibility.Visible;
                    EditablePathBox.Visibility = Visibility.Collapsed;
                    await NavigateToFolderAsync(path);
                }
                else
                {
                    // Invalid path, revert
                    PathBreadcrumbBar.Visibility = Visibility.Visible;
                    EditablePathBox.Visibility = Visibility.Collapsed;
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Escape)
            {
                PathBreadcrumbBar.Visibility = Visibility.Visible;
                EditablePathBox.Visibility = Visibility.Collapsed;
            }
        }

        private void EditablePathBox_LostFocus(object sender, RoutedEventArgs e)
        {
            PathBreadcrumbBar.Visibility = Visibility.Visible;
            EditablePathBox.Visibility = Visibility.Collapsed;
        }

        private void LargeOldFilterButton_Click(object sender, RoutedEventArgs e)
        {
            SortData();
        }

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPivot.SelectedItem is PivotItem pi)
            {
                string header = pi.Header?.ToString() ?? "";
                if (header == "Visual Chart")
                {
                    if (ResultsGrid.ItemsSource is ObservableCollection<GridItemViewModel> filtered)
                        UpdateChart(filtered.ToList());
                }
                else if (header == "Donut Chart")
                {
                    if (ResultsGrid.ItemsSource is ObservableCollection<GridItemViewModel> filtered)
                        RefreshDonutChart(filtered.ToList());
                }
                else if (header == "Top Files")
                {
                    if (TopFilesGrid.ItemsSource != _topFiles)
                    {
                        TopFilesGrid.ItemsSource = _topFiles;
                    }

                    if (!_topFilesScanned)
                    {
                        _ = ScanTopFilesAsync();
                    }
                }
            }
        }

        private void ScanTopFiles_Click(object sender, RoutedEventArgs e)
        {
            _ = ScanTopFilesAsync();
        }

        private async Task ScanTopFilesAsync()
        {
            string scanPath = !string.IsNullOrEmpty(_currentPath)
                ? _currentPath
                : (Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\");

            TopFilesProgress.IsActive = true;
            TopFilesProgress.Visibility = Visibility.Visible;
            _topFiles.Clear();
            TopFilesGrid.ItemsSource = _topFiles;
            _topFilesScanned = true;
            TopFilesStatusText.Text = $"Scanning {scanPath}...";

            bool includeHidden = true; // Always include system and hidden files (hiberfil.sys, pagefile.sys, swapfile.sys)

            try
            {
                var progress = new Progress<List<DiskItemInfo>>(items =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        var oc = new ObservableCollection<TopFileResult>();
                        foreach (var f in items)
                        {
                            oc.Add(new TopFileResult
                            {
                                Name = f.Name,
                                FullPath = f.FullPath,
                                SizeBytes = f.SizeBytes,
                                FormattedSize = DiskAnalyzerHelper.FormatSize(f.SizeBytes),
                                LastModified = f.LastModified,
                                FormattedDate = f.LastModified.ToString("M/d/yyyy h:mm:ss tt"),
                                IconSource = null
                            });
                        }
                        _topFiles = oc;
                        TopFilesGrid.ItemsSource = _topFiles;

                        if (items.Count > 0)
                        {
                            TopFilesStatusText.Text = $"Scanning... Found {items.Count} large files (Largest: {items[0].Name} \u2014 {DiskAnalyzerHelper.FormatSize(items[0].SizeBytes)})";
                        }
                    });
                });

                var files = await Task.Run(() => DiskAnalyzerHelper.FindLargestFiles(scanPath, 100, includeHidden, progress));

                DispatcherQueue.TryEnqueue(() =>
                {
                    var oc = new ObservableCollection<TopFileResult>();
                    foreach (var f in files)
                    {
                        oc.Add(new TopFileResult
                        {
                            Name = f.Name,
                            FullPath = f.FullPath,
                            SizeBytes = f.SizeBytes,
                            FormattedSize = DiskAnalyzerHelper.FormatSize(f.SizeBytes),
                            LastModified = f.LastModified,
                            FormattedDate = f.LastModified.ToString("M/d/yyyy h:mm:ss tt"),
                            IconSource = null
                        });
                    }
                    _topFiles = oc;
                    TopFilesGrid.ItemsSource = _topFiles;
                    TopFilesStatusText.Text = $"Scan complete \u2014 Showing {_topFiles.Count} largest files across {scanPath}";

                    var snapshot = oc.ToList();
                    _ = Task.Run(async () =>
                    {
                        foreach (var vm in snapshot)
                        {
                            try
                            {
                                var icon = await IconUtilities.GetIconAsync(vm.FullPath, false, DispatcherQueue);
                                if (icon != null)
                                {
                                    DispatcherQueue.TryEnqueue(() => vm.IconSource = icon);
                                }
                            }
                            catch { }
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                App.WriteCrashLog(ex);
                DispatcherQueue.TryEnqueue(() =>
                {
                    TopFilesStatusText.Text = "Scan error: " + ex.Message;
                });
            }
            finally
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    TopFilesProgress.IsActive = false;
                    TopFilesProgress.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void TopFilesGrid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (TopFilesGrid.SelectedItem is TopFileResult item && !string.IsNullOrEmpty(item.FullPath))
            {
                try
                {
                    using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{item.FullPath}\"",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        private void TopFiles_OpenExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (TopFilesGrid.SelectedItem is TopFileResult item && !string.IsNullOrEmpty(item.FullPath))
            {
                try
                {
                    using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{item.FullPath}\"",
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        private void TopFiles_CopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (TopFilesGrid.SelectedItem is TopFileResult item && !string.IsNullOrEmpty(item.FullPath))
            {
                var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                package.SetText(item.FullPath);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
            }
        }

        private async void DataGrid_SendToRecycleBin_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsGrid.SelectedItem is GridItemViewModel vm)
            {
                var dialog = new ContentDialog
                {
                    Title = "Send to Recycle Bin",
                    Content = $"Send {vm.Name} to the Recycle Bin?",
                    PrimaryButtonText = "Send to Recycle Bin",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            if (vm.IsFile)
                            {
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(vm.FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            }
                            else
                            {
                                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(vm.FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                            }
                        });

                        _currentItems.Remove(vm);
                        SortData();

                        var infoBar = new InfoBar
                        {
                            Severity = InfoBarSeverity.Success,
                            Title = "Success",
                            Message = $"{vm.Name} moved to Recycle Bin.",
                            IsOpen = true,
                            Margin = new Thickness(0, 0, 0, 12)
                        };
                        var parentPanel = AdminWarningBar.Parent as StackPanel;
                        if (parentPanel != null)
                        {
                            parentPanel.Children.Insert(0, infoBar);
                            _ = Task.Delay(3000).ContinueWith(_ => DispatcherQueue.TryEnqueue(() => parentPanel.Children.Remove(infoBar)));
                        }
                    }
                    catch (Exception ex)
                    {
                        var errDialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = $"Failed to send to Recycle Bin: {ex.Message}",
                            CloseButtonText = "OK",
                            XamlRoot = this.XamlRoot
                        };
                        await errDialog.ShowAsync();
                    }
                }
            }
        }

        private void RefreshDonutChart(List<GridItemViewModel> items)
        {
            if (DonutCanvas == null || DonutLegendList == null) return;

            DonutCanvas.Children.Clear();
            if (items == null || items.Count == 0)
            {
                DonutLegendList.ItemsSource = null;
                return;
            }

            double radiusOuter = 160;
            double radiusInner = 100;
            double centerX = 160;
            double centerY = 160;

            var chartItems = new List<ChartItemViewModel>();
            var colors = new[]
            {
                Microsoft.UI.ColorHelper.FromArgb(255, 100, 210, 255),
                Microsoft.UI.ColorHelper.FromArgb(255, 48, 209, 88),
                Microsoft.UI.ColorHelper.FromArgb(255, 255, 159, 10),
                Microsoft.UI.ColorHelper.FromArgb(255, 255, 69, 58),
                Microsoft.UI.ColorHelper.FromArgb(255, 191, 90, 242),
                Microsoft.UI.ColorHelper.FromArgb(255, 255, 214, 10),
                Microsoft.UI.ColorHelper.FromArgb(255, 107, 102, 255),
                Microsoft.UI.ColorHelper.FromArgb(255, 174, 174, 178)
            };

            var sortedItems = items.OrderByDescending(i => i.SizeBytes).ToList();
            int topLimit = 8;
            var topItems = sortedItems.Take(topLimit).ToList();
            var remainingItems = sortedItems.Skip(topLimit).ToList();

            long totalSize = items.Sum(i => i.SizeBytes);
            if (totalSize == 0) return;

            double startAngle = -90;
            int colorIdx = 0;
            
            Action<double, Color, ChartItemViewModel> drawArc = (size, col, vm) =>
            {
                double sweepAngle = (size / (double)totalSize) * 360;
                if (sweepAngle < 0.1) return;

                double endAngle = startAngle + sweepAngle;

                double startOuterX = centerX + radiusOuter * Math.Cos(startAngle * Math.PI / 180);
                double startOuterY = centerY + radiusOuter * Math.Sin(startAngle * Math.PI / 180);
                double endOuterX = centerX + radiusOuter * Math.Cos(endAngle * Math.PI / 180);
                double endOuterY = centerY + radiusOuter * Math.Sin(endAngle * Math.PI / 180);

                double startInnerX = centerX + radiusInner * Math.Cos(startAngle * Math.PI / 180);
                double startInnerY = centerY + radiusInner * Math.Sin(startAngle * Math.PI / 180);
                double endInnerX = centerX + radiusInner * Math.Cos(endAngle * Math.PI / 180);
                double endInnerY = centerY + radiusInner * Math.Sin(endAngle * Math.PI / 180);

                bool isLargeArc = sweepAngle > 180;

                var pathFigure = new Microsoft.UI.Xaml.Media.PathFigure
                {
                    StartPoint = new Windows.Foundation.Point(startInnerX, startInnerY),
                    IsClosed = true
                };

                pathFigure.Segments.Add(new Microsoft.UI.Xaml.Media.LineSegment { Point = new Windows.Foundation.Point(startOuterX, startOuterY) });
                pathFigure.Segments.Add(new Microsoft.UI.Xaml.Media.ArcSegment
                {
                    Point = new Windows.Foundation.Point(endOuterX, endOuterY),
                    Size = new Windows.Foundation.Size(radiusOuter, radiusOuter),
                    IsLargeArc = isLargeArc,
                    SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Clockwise
                });
                pathFigure.Segments.Add(new Microsoft.UI.Xaml.Media.LineSegment { Point = new Windows.Foundation.Point(endInnerX, endInnerY) });
                pathFigure.Segments.Add(new Microsoft.UI.Xaml.Media.ArcSegment
                {
                    Point = new Windows.Foundation.Point(startInnerX, startInnerY),
                    Size = new Windows.Foundation.Size(radiusInner, radiusInner),
                    IsLargeArc = isLargeArc,
                    SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Counterclockwise
                });

                var pathGeometry = new Microsoft.UI.Xaml.Media.PathGeometry();
                pathGeometry.Figures.Add(pathFigure);

                var path = new Microsoft.UI.Xaml.Shapes.Path
                {
                    Data = pathGeometry,
                    Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(col),
                    Stroke = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
                    StrokeThickness = 2,
                    DataContext = vm
                };
                
                path.Tapped += ChartItem_Tapped;
                ToolTipService.SetToolTip(path, vm.ToolTip);

                DonutCanvas.Children.Add(path);

                startAngle = endAngle;
            };

            foreach (var item in topItems)
            {
                if (item.SizeBytes == 0) continue;
                var col = colors[colorIdx % colors.Length];
                var vm = new ChartItemViewModel
                {
                    Name = item.Name,
                    FullPath = item.FullPath,
                    IsFile = item.IsFile,
                    Color = new Microsoft.UI.Xaml.Media.SolidColorBrush(col),
                    ToolTip = $"{item.Name} - {item.FormattedSize} ({item.FormattedPercentage})",
                    FormattedSize = item.FormattedSize
                };
                chartItems.Add(vm);
                drawArc(item.SizeBytes, col, vm);
                colorIdx++;
            }

            if (remainingItems.Count > 0)
            {
                long otherSizeBytes = remainingItems.Sum(i => i.SizeBytes);
                if (otherSizeBytes > 0)
                {
                    var col = Microsoft.UI.ColorHelper.FromArgb(255, 120, 120, 128);
                    var formattedOtherSize = DiskAnalyzerHelper.FormatSize(otherSizeBytes);
                    var vm = new ChartItemViewModel
                    {
                        Name = $"Other ({remainingItems.Count} items)",
                        FullPath = "",
                        IsFile = false,
                        Color = new Microsoft.UI.Xaml.Media.SolidColorBrush(col),
                        ToolTip = $"Other {remainingItems.Count} items - {formattedOtherSize}",
                        FormattedSize = formattedOtherSize
                    };
                    chartItems.Add(vm);
                    drawArc(otherSizeBytes, col, vm);
                }
            }

            var tb = new TextBlock
            {
                Text = DiskAnalyzerHelper.FormatSize(totalSize),
                FontSize = 24,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
            };
            
            tb.SizeChanged += (s, e) =>
            {
                Canvas.SetLeft(tb, centerX - (tb.ActualWidth / 2));
                Canvas.SetTop(tb, centerY - (tb.ActualHeight / 2));
            };
            DonutCanvas.Children.Add(tb);

            DonutLegendList.ItemsSource = chartItems;
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
        public string FullPath { get; set; } = "";
        public bool IsFile { get; set; }
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

    public class TopFileResult : System.ComponentModel.INotifyPropertyChanged
    {
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public long SizeBytes { get; set; }
        public string FormattedSize { get; set; } = "";
        public DateTime LastModified { get; set; }
        public string FormattedDate { get; set; } = "";

        private Microsoft.UI.Xaml.Media.ImageSource? _iconSource;
        public Microsoft.UI.Xaml.Media.ImageSource? IconSource
        {
            get => _iconSource;
            set { _iconSource = value; OnPropertyChanged(); }
        }
    }

    public class TypeCategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string FormattedSize { get; set; } = string.Empty;
        public double Percentage { get; set; }
        public Microsoft.UI.Xaml.Media.SolidColorBrush ColorBrush { get; set; } = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.CornflowerBlue);
    }
}
