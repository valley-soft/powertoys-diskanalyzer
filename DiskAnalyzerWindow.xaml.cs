using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer
{
    public partial class DiskAnalyzerWindow : Window
    {
        private string _rootPath;
        public ObservableCollection<FolderNode> RootFolders { get; set; } = new ObservableCollection<FolderNode>();

        public DiskAnalyzerWindow(string rootPath)
        {
            InitializeComponent();
            _rootPath = string.IsNullOrWhiteSpace(rootPath) || rootPath == "gui" ? "C:\\" : rootPath;
            
            FolderTree.ItemsSource = RootFolders;
            LoadTree(_rootPath);
        }

        private async void LoadTree(string path)
        {
            try
            {
                var rootNode = new FolderNode { Name = path, FullPath = path };
                RootFolders.Add(rootNode);

                // Load initial children async to keep UI responsive
                await Task.Run(() => rootNode.LoadChildren());
                
                FolderTree.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading path: {ex.Message}");
            }
        }

        private void FolderTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is FolderNode node)
            {
                CurrentPathText.Text = node.FullPath;
                LoadGridData(node.FullPath);
            }
        }

        private async void LoadGridData(string path)
        {
            ItemsGrid.ItemsSource = null;
            try
            {
                var items = await Task.Run(() => DiskAnalyzerHelper.ScanDirectory(path, 1, false));
                
                var viewModels = items.Select(i => new GridItemViewModel
                {
                    Name = i.Name,
                    FullPath = i.FullPath,
                    FormattedSize = DiskAnalyzerHelper.FormatSize(i.SizeBytes),
                    FormattedAllocated = DiskAnalyzerHelper.FormatSize(i.AllocatedSizeBytes),
                    ItemCount = i.ItemCount,
                    LastModified = i.LastModified
                }).OrderByDescending(v => v.FormattedSize).ToList();

                ItemsGrid.ItemsSource = viewModels;
            }
            catch
            {
                // Ignore access errors on selection
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (FolderTree.SelectedItem is FolderNode node)
            {
                LoadGridData(node.FullPath);
            }
        }
    }

    public class FolderNode
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public ObservableCollection<FolderNode> Children { get; set; } = new ObservableCollection<FolderNode>();

        public void LoadChildren()
        {
            try
            {
                var dirs = Directory.GetDirectories(FullPath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Children.Clear();
                    foreach (var d in dirs)
                    {
                        Children.Add(new FolderNode { Name = Path.GetFileName(d), FullPath = d });
                    }
                });
            }
            catch
            {
                // Access denied usually
            }
        }
    }

    public class GridItemViewModel
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string FormattedSize { get; set; }
        public string FormattedAllocated { get; set; }
        public int ItemCount { get; set; }
        public DateTime LastModified { get; set; }
    }
}
