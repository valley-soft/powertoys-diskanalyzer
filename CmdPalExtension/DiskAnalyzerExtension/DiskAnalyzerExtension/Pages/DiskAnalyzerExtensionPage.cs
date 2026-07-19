// Copyright (c) Thet Naing Saw. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Community.PowerToys.Run.Plugin.DiskAnalyzer;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DiskAnalyzerExtension
{
    /// <summary>
    /// Single ListPage that drives its own internal navigation via a state machine.
    /// Clicking a menu item → changes mode → RaiseItemsChanged → new items appear.
    /// A "← Back" item always appears at the top of sub-views.
    /// SearchText is cleared on every mode change so CmdPal's built-in filter never hides results.
    /// </summary>
    public sealed partial class DiskAnalyzerExtensionPage : ListPage
    {
        // ── State ─────────────────────────────────────────────────────────────
        public enum PageMode { MainMenu, Drives, Scanning, TopFolders, LargestFiles, ExtFiles, EmptyFolders }

        private PageMode _mode    = PageMode.MainMenu;
        private string   _path    = string.Empty;
        private string   _ext     = string.Empty;

        // Cached results for async operations
        private IListItem[]? _asyncCache;
        private bool          _asyncRunning;

        public DiskAnalyzerExtensionPage()
        {
            Icon        = SafeIcon("Assets\\DiskAnalyzerLight.png", "\ue71b");
            Title       = "ValleySoft Disk Analyzer";
            Name        = "ValleySoft Disk Analyzer";
            ShowDetails = false;
            PlaceholderText = "Select a command below";
        }

        internal static IconInfo SafeIcon(string relativePath, string fallbackGlyph = "\ue71b")
        {
            try
            {
                var icon = IconHelpers.FromRelativePath(relativePath);
                return icon ?? new IconInfo(fallbackGlyph);
            }
            catch
            {
                return new IconInfo(fallbackGlyph);
            }
        }

        // ── Navigate helper ───────────────────────────────────────────────────
        private void GoTo(PageMode mode, string path = "", string ext = "")
        {
            _mode         = mode;
            _path         = path;
            _ext          = ext;
            _asyncCache   = null;
            _asyncRunning = false;
            SearchText    = string.Empty;   // clear search box so filter doesn't hide items
            RaiseItemsChanged();
        }

        private IListItem BackItem() =>
            new ListItem(new MySetModeCommand(this, PageMode.MainMenu))
            {
                Title = "← Back to main menu",
                Icon  = new IconInfo("\ue72b"),
            };

        // ── Main dispatcher ───────────────────────────────────────────────────
        public override IListItem[] GetItems()
        {
            return _mode switch
            {
                PageMode.MainMenu    => MainMenuItems(),
                PageMode.Drives      => DriveItems(),
                PageMode.Scanning    => AsyncItems(() => FolderScanItems(_path)),
                PageMode.TopFolders  => AsyncItems(() => TopFolderItems(_path)),
                PageMode.LargestFiles => AsyncItems(() => LargestFileItems(_path)),
                PageMode.ExtFiles    => AsyncItems(() => ExtensionItems(_path, _ext)),
                PageMode.EmptyFolders => AsyncItems(() => EmptyFolderItems(_path)),
                _                    => MainMenuItems(),
            };
        }

        // ── Async wrapper ─────────────────────────────────────────────────────
        private IListItem[] AsyncItems(Func<IListItem[]> buildFn)
        {
            if (_asyncCache != null)
                return PrependBack(_asyncCache);

            if (_asyncRunning)
                return new[] { PlaceholderItem("Working… please wait.") };

            _asyncRunning = true;
            System.Threading.Tasks.Task.Run(() =>
            {
                try   { _asyncCache = buildFn(); }
                catch (Exception ex) { _asyncCache = new[] { PlaceholderItem($"Error: {ex.Message}") }; }
                finally { _asyncRunning = false; }
                RaiseItemsChanged();
            });

            return new[] { PlaceholderItem("Working… please wait.") };
        }

        private IListItem[] PrependBack(IListItem[] items)
        {
            var list = new List<IListItem> { BackItem() };
            list.AddRange(items);
            return list.ToArray();
        }

        // ── MAIN MENU ─────────────────────────────────────────────────────────
        private IListItem[] MainMenuItems() => new IListItem[]
        {
            new ListItem(new MyNoOpCommand())
            {
                Title    = "ValleySoft Disk Analyzer",
                Subtitle = "Select a command",
                Icon     = new IconInfo("\ue71b"),
            },
            new ListItem(new MySetModeCommand(this, PageMode.Drives))
            {
                Title    = "drives",
                Subtitle = "List all drives with used / free / total space",
                Icon     = new IconInfo("\ue71b"),
            },
            new ListItem(new MySetModeCommand(this, PageMode.TopFolders, "C:\\"))
            {
                Title    = "top C:\\",
                Subtitle = "Top-level folders ranked by size",
                Icon     = new IconInfo("\ue71b"),
            },
            new ListItem(new MySetModeCommand(this, PageMode.LargestFiles, "C:\\"))
            {
                Title    = "largest C:\\",
                Subtitle = "Find the largest files recursively",
                Icon     = new IconInfo("\ue71b"),
            },
            new ListItem(new MySetModeCommand(this, PageMode.ExtFiles, "C:\\", ".mp4"))
            {
                Title    = "ext C:\\ .mp4",
                Subtitle = "Find largest files of a specific extension",
                Icon     = new IconInfo("\ue71b"),
            },
            new ListItem(new MySetModeCommand(this, PageMode.EmptyFolders, "C:\\"))
            {
                Title    = "empty C:\\",
                Subtitle = "Find empty folders",
                Icon     = new IconInfo("\ue71b"),
            },
            new ListItem(new MySetModeCommand(this, PageMode.Scanning, "C:\\Users"))
            {
                Title    = "C:\\Users",
                Subtitle = "Scan any folder – ranked by size",
                Icon     = new IconInfo("\ue71b"),
            },
        };

        // ── DRIVES ────────────────────────────────────────────────────────────
        private IListItem[] DriveItems()
        {
            var drives = System.IO.DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .Select(d =>
                {
                    try
                    {
                        return new { Drive = d, Used = d.TotalSize - d.AvailableFreeSpace };
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return null;
                    }
                    catch (System.IO.IOException)
                    {
                        return null;
                    }
                })
                .Where(d => d != null)
                .OrderByDescending(d => d!.Used)
                .Select(d => d!.Drive);

            var items = new List<IListItem> { BackItem() };

            foreach (var drive in drives)
            {
                var used      = drive.TotalSize - drive.AvailableFreeSpace;
                var free      = drive.AvailableFreeSpace;
                var pct       = (double)used / drive.TotalSize * 100;
                var bar       = ProgressBar(pct);
                var name      = drive.Name;

                var copyCmd  = new CommandContextItem(new MyCopyTextCommand(name))                          { Title = "Copy path" };
                var scanCmd  = new CommandContextItem(new MySetModeCommand(this, PageMode.Scanning, name))  { Title = "Scan this drive" };
                var topCmd   = new CommandContextItem(new MySetModeCommand(this, PageMode.TopFolders, name)) { Title = "Top folders" };
                var lgstCmd  = new CommandContextItem(new MySetModeCommand(this, PageMode.LargestFiles, name)) { Title = "Largest files" };

                items.Add(new ListItem(new MySetModeCommand(this, PageMode.Scanning, name))
                {
                    Title        = $"{name}  {Fmt(used)} / {Fmt(drive.TotalSize)}  ({pct:F1}%)",
                    Subtitle     = $"{bar}  Free: {Fmt(free)}",
                    Icon         = new IconInfo("\ue71b"),
                    MoreCommands = new[] { scanCmd, topCmd, lgstCmd, copyCmd },
                });
            }
            return items.ToArray();
        }

        // ── FOLDER SCAN ───────────────────────────────────────────────────────
        private IListItem[] FolderScanItems(string path)
        {
            var results = Community.PowerToys.Run.Plugin.DiskAnalyzer.DiskAnalyzerHelper.ScanDirectory(path, 1, true);
            if (results.Count == 0)
                return new[] { PlaceholderItem($"No items found in '{path}'") };

            var totalSize = results.Sum(f => f.SizeBytes);
            var items     = new List<IListItem>
            {
                new ListItem(new MyNoOpCommand())
                {
                    Title    = $"📂 {path}",
                    Subtitle = $"Total: {Fmt(totalSize)}  –  {results.Count} items",
                    Icon     = new IconInfo("\ue71b"),
                }
            };

            foreach (var item in results.OrderByDescending(r => r.SizeBytes))
            {
                var pct          = totalSize > 0 ? (double)item.SizeBytes / totalSize * 100 : 0;
                var bar          = ProgressBar(pct);
                var capturedPath = item.FullPath;
                var isFile       = item.IsFile;

                var copyPath = new CommandContextItem(new MyCopyTextCommand(capturedPath))   { Title = "Copy path" };
                var copySize = new CommandContextItem(new MyCopyTextCommand(Fmt(item.SizeBytes))) { Title = "Copy size" };
                var openExp  = new CommandContextItem(new MyOpenExplorerCommand(capturedPath, isFile)) { Title = "Open in File Explorer" };
                var moreCommands = new List<CommandContextItem> { openExp, copyPath, copySize };

                if (!isFile)
                {
                    moreCommands.Insert(0, new CommandContextItem(new MySetModeCommand(this, PageMode.Scanning, capturedPath))     { Title = "Drill down" });
                    moreCommands.Insert(1, new CommandContextItem(new MySetModeCommand(this, PageMode.LargestFiles, capturedPath)) { Title = "Find largest files" });
                    moreCommands.Insert(2, new CommandContextItem(new MySetModeCommand(this, PageMode.TopFolders, capturedPath))   { Title = "Top subfolders" });
                }

                ICommand primaryCmd = isFile
                    ? (ICommand)new MyOpenExplorerCommand(capturedPath, true)
                    : new MySetModeCommand(this, PageMode.Scanning, capturedPath);

                items.Add(new ListItem(primaryCmd)
                {
                    Title        = $"{(isFile ? "📄" : "📁")} {item.Name}",
                    Subtitle     = $"{bar} {Fmt(item.SizeBytes)}  ({pct:F1}%)  Allocated: {Fmt(item.AllocatedSizeBytes)}",
                    Icon         = new IconInfo("\ue71b"),
                    MoreCommands = moreCommands.ToArray(),
                });
            }
            return items.ToArray();
        }

        // ── TOP FOLDERS ───────────────────────────────────────────────────────
        private IListItem[] TopFolderItems(string path)
        {
            if (!System.IO.Directory.Exists(path))
                return new[] { PlaceholderItem($"Path not found: {path}") };

            var results   = Community.PowerToys.Run.Plugin.DiskAnalyzer.DiskAnalyzerHelper.GetTopFolders(path, 20, 1, true);
            var totalSize = results.Sum(f => f.SizeBytes);

            var items = new List<IListItem>
            {
                new ListItem(new MyNoOpCommand())
                {
                    Title    = $"📊 Top folders in {path}",
                    Subtitle = $"Total: {Fmt(totalSize)}  –  {results.Count} folders",
                    Icon     = new IconInfo("\ue71b"),
                }
            };

            foreach (var item in results)
            {
                var pct          = totalSize > 0 ? (double)item.SizeBytes / totalSize * 100 : 0;
                var bar          = ProgressBar(pct);
                var capturedPath = item.FullPath;

                items.Add(new ListItem(new MySetModeCommand(this, PageMode.Scanning, capturedPath))
                {
                    Title        = $"📁 {item.Name}",
                    Subtitle     = $"{DiskAnalyzerHelper.FormatSize(item.SizeBytes)} ({pct:F1}%) | Allocated: {DiskAnalyzerHelper.FormatSize(item.AllocatedSizeBytes)} | Items: {item.FileCount + item.FolderCount} | {item.FullPath}",
                    Icon         = new IconInfo("\ue71b"),
                    MoreCommands = new CommandContextItem[]
                    {
                        new CommandContextItem(new MySetModeCommand(this, PageMode.Scanning, capturedPath))    { Title = "Drill down" },
                        new CommandContextItem(new MySetModeCommand(this, PageMode.LargestFiles, capturedPath)) { Title = "Find largest files" },
                        new CommandContextItem(new MyOpenExplorerCommand(capturedPath, false))                  { Title = "Open in File Explorer" },
                        new CommandContextItem(new MyCopyTextCommand(capturedPath))                             { Title = "Copy path" },
                    },
                });
            }
            return items.ToArray();
        }

        // ── LARGEST FILES ─────────────────────────────────────────────────────
        private IListItem[] LargestFileItems(string path)
        {
            if (!System.IO.Directory.Exists(path))
                return new[] { PlaceholderItem($"Path not found: {path}") };

            var results = Community.PowerToys.Run.Plugin.DiskAnalyzer.DiskAnalyzerHelper.FindLargestFiles(path, 20, true);
            if (results.Count == 0)
                return new[] { PlaceholderItem($"No files found in '{path}'") };

            var items = new List<IListItem>
            {
                new ListItem(new MyNoOpCommand())
                {
                    Title    = $"🔍 Largest files in {path}",
                    Subtitle = $"{results.Count} files found",
                    Icon     = new IconInfo("\ue71b"),
                }
            };

            foreach (var item in results)
            {
                var capturedPath = item.FullPath;
                items.Add(new ListItem(new MyOpenExplorerCommand(capturedPath, true))
                {
                    Title        = $"📄 {item.Name}",
                    Subtitle     = $"{Fmt(item.SizeBytes)}  Allocated: {Fmt(item.AllocatedSizeBytes)}  –  {capturedPath}",
                    Icon         = new IconInfo("\ue71b"),
                    MoreCommands = new CommandContextItem[]
                    {
                        new CommandContextItem(new MyOpenExplorerCommand(capturedPath, true))                { Title = "Open in File Explorer" },
                        new CommandContextItem(new MyCopyTextCommand(capturedPath))                         { Title = "Copy path" },
                        new CommandContextItem(new MyCopyTextCommand(Fmt(item.SizeBytes)))                  { Title = "Copy size" },
                    },
                });
            }
            return items.ToArray();
        }

        // ── EXTENSION FILES ───────────────────────────────────────────────────
        private IListItem[] ExtensionItems(string path, string ext)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(ext))
                return new[] { PlaceholderItem("Usage: provide path and extension") };
            if (!System.IO.Directory.Exists(path))
                return new[] { PlaceholderItem($"Path not found: {path}") };

            var results   = Community.PowerToys.Run.Plugin.DiskAnalyzer.DiskAnalyzerHelper.FindFilesByExtension(path, ext, 20, true);
            if (results.Count == 0)
                return new[] { PlaceholderItem($"No {ext} files found in '{path}'") };

            var totalSize = results.Sum(f => f.SizeBytes);
            var items = new List<IListItem>
            {
                new ListItem(new MyNoOpCommand())
                {
                    Title    = $"🔎 {ext} files in {path}",
                    Subtitle = $"Total: {Fmt(totalSize)}  –  {results.Count} files",
                    Icon     = new IconInfo("\ue71b"),
                }
            };

            foreach (var item in results)
            {
                var capturedPath = item.FullPath;
                items.Add(new ListItem(new MyOpenExplorerCommand(capturedPath, true))
                {
                    Title        = $"📄 {item.Name}",
                    Subtitle     = $"{Fmt(item.SizeBytes)}  Allocated: {Fmt(item.AllocatedSizeBytes)}  –  {capturedPath}",
                    Icon         = new IconInfo("\ue71b"),
                    MoreCommands = new CommandContextItem[]
                    {
                        new CommandContextItem(new MyOpenExplorerCommand(capturedPath, true)) { Title = "Open in File Explorer" },
                        new CommandContextItem(new MyCopyTextCommand(capturedPath))           { Title = "Copy path" },
                    },
                });
            }
            return items.ToArray();
        }

        // ── EMPTY FOLDERS ─────────────────────────────────────────────────────
        private IListItem[] EmptyFolderItems(string path)
        {
            if (!System.IO.Directory.Exists(path))
                return new[] { PlaceholderItem($"Path not found: {path}") };

            var results = Community.PowerToys.Run.Plugin.DiskAnalyzer.DiskAnalyzerHelper.FindEmptyFolders(path, 30, true);
            if (results.Count == 0)
                return new[] { PlaceholderItem($"No empty folders found in '{path}'") };

            var items = new List<IListItem>
            {
                new ListItem(new MyNoOpCommand())
                {
                    Title    = $"📁 Empty folders in {path}",
                    Subtitle = $"{results.Count} empty folders found",
                    Icon     = new IconInfo("\ue71b"),
                }
            };

            foreach (var item in results)
            {
                var capturedPath = item.FullPath;
                items.Add(new ListItem(new MyOpenExplorerCommand(capturedPath, false))
                {
                    Title        = $"📁 {item.Name}",
                    Subtitle     = capturedPath,
                    Icon         = new IconInfo("\ue71b"),
                    MoreCommands = new CommandContextItem[]
                    {
                        new CommandContextItem(new MyOpenExplorerCommand(capturedPath, false)) { Title = "Open in File Explorer" },
                        new CommandContextItem(new MyCopyTextCommand(capturedPath))            { Title = "Copy path" },
                    },
                });
            }
            return items.ToArray();
        }

        // ── Shared helpers ────────────────────────────────────────────────────
        internal void SetMode(PageMode mode, string path = "", string ext = "") =>
            GoTo(mode, path, ext);

        private static string Fmt(long bytes) =>
            Community.PowerToys.Run.Plugin.DiskAnalyzer.DiskAnalyzerHelper.FormatSize(bytes);

        private static string ProgressBar(double pct)
        {
            const int total = 10;
            int filled = (int)Math.Round(pct / 100.0 * total);
            string block = pct < 70 ? "🟩" : pct < 90 ? "🟨" : "🟥";
            return string.Concat(Enumerable.Range(0, total).Select(i => i < filled ? block : "⬜"));
        }

        private static IListItem PlaceholderItem(string msg) =>
            new ListItem(new MyNoOpCommand()) { Title = msg };
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public sealed partial class MyNoOpCommand : InvokableCommand
    {
        public MyNoOpCommand() { Name = "Select"; }
        public override ICommandResult Invoke() => CommandResult.KeepOpen();
    }

    /// <summary>Changes the page mode, clears search, and raises items changed.</summary>
    public sealed partial class MySetModeCommand : InvokableCommand
    {
        private readonly DiskAnalyzerExtensionPage             _page;
        private readonly DiskAnalyzerExtensionPage.PageMode    _mode;
        private readonly string _path;
        private readonly string _ext;

        public MySetModeCommand(DiskAnalyzerExtensionPage page,
                                DiskAnalyzerExtensionPage.PageMode mode,
                                string path = "", string ext = "")
        {
            _page = page; _mode = mode; _path = path; _ext = ext;
            Name = "Open";
        }

        public override ICommandResult Invoke()
        {
            _page.SetMode(_mode, _path, _ext);
            return CommandResult.KeepOpen();
        }
    }

    public sealed partial class MyOpenExplorerCommand : InvokableCommand
    {
        private readonly string _path;
        private readonly bool   _isFile;
        public MyOpenExplorerCommand(string path, bool isFile)
        { _path = path; _isFile = isFile; Name = "Open"; }

        public override ICommandResult Invoke()
        {
            var args = _isFile ? $"/select,\"{_path}\"" : $"\"{_path}\"";
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("explorer.exe", args)
                { UseShellExecute = true });
            return CommandResult.Dismiss();
        }
    }

    public sealed partial class MyAnonymousCommand : InvokableCommand
    {
        private readonly System.Action _action;
        public MyAnonymousCommand(System.Action action) { _action = action; Name = "Invoke"; }
        public override ICommandResult Invoke() { _action?.Invoke(); return CommandResult.Dismiss(); }
    }

    public sealed partial class MyCopyTextCommand : InvokableCommand
    {
        public string Text { get; set; }
        public MyCopyTextCommand(string text)
        { Text = text; Name = "Copy"; Icon = new IconInfo("\ue8c8"); }
        public override ICommandResult Invoke()
        {
            ClipboardHelper.SetText(Text);
            return CommandResult.ShowToast("Copied to clipboard");
        }
    }
}
