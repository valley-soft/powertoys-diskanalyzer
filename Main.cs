// Copyright (c) Thet. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer
{
    /// <summary>
    /// PowerToys Run plugin that provides TreeSize-like disk space analysis.
    /// Activation keyword: ds
    /// </summary>
    public class Main : IPlugin, IDelayedExecutionPlugin, IContextMenu, ISettingProvider, IDisposable
    {
        public static string PluginID => "B4F2E8A1C3D64F7E9A1B2C3D4E5F6A7B";
        public string Name => "ValleySoft Disk Analyzer (Runs Plug-in)";
        public string Description => "Analyze disk space usage like TreeSize. Scan folders, find large files, and view drive info.";

        // Fix #5: Command name constants â€” no more magic strings
        private const string CmdDrives = "drives";
        private const string CmdLargest = "largest ";
        private const string CmdTop = "top ";
        private const string CmdExt = "ext ";
        private const string CmdEmpty = "empty ";
        private const string CmdGui = "gui";

        // Fix #4: Scan result cache â€” 10-second TTL avoids redundant re-scans
        private static readonly ConcurrentDictionary<string, (DateTime Timestamp, List<Result> Results)> _scanCache = new();
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(10);

        private PluginInitContext? _context;
        private string? _iconPath;
        private bool _disposed;

        // Settings
        private int _maxResults = 15;
        private int _maxDepth = 1;
        private bool _includeHiddenFiles = true;
        private bool _showPercentage = true;

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        /// <summary>
        /// Fast initial results â€” shows command hints.
        /// </summary>
        public List<Result> Query(Query query)
        {


            var search = query.Search?.Trim() ?? string.Empty;

            // Strip "ds " prefix if ChangeQuery doubled it
            if (search.StartsWith("ds ", StringComparison.OrdinalIgnoreCase))
                search = search.Substring(3).Trim();

            if (string.IsNullOrEmpty(search))
            {
                return GetHelpResults();
            }

            // "drives" command â€” fast, no delayed execution needed
            if (search.Equals(CmdDrives, StringComparison.OrdinalIgnoreCase))
            {
                return GetDriveResults();
            }

            // "gui" command â€” opens standalone window
            if (search.StartsWith(CmdGui, StringComparison.OrdinalIgnoreCase))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Open DiskAnalyzer Window",
                        SubTitle = "Launch the full standalone graphical user interface",
                        IcoPath = _iconPath,
                        Score = 1000,
                        Action = _ =>
                        {
                            var path = search.Length > 3 ? search[3..].Trim().Trim('"') : null;
                            try
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var window = new DiskAnalyzerWindow(_context?.API?.GetCurrentTheme() ?? Theme.Light, path);
                                    window.Show();
                                });
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Failed to launch WPF GUI: {ex.Message}", GetType());
                            }
                            return true;
                        }
                    }
                };
            }

            return new List<Result>();
        }

        /// <summary>
        /// Delayed execution for expensive disk I/O operations.
        /// </summary>
        public List<Result> Query(Query query, bool delayedExecution)
        {
            if (!delayedExecution)
            {
                return new List<Result>();
            }



            var search = query.Search?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(search) || 
                search.Equals(CmdDrives, StringComparison.OrdinalIgnoreCase) ||
                search.StartsWith(CmdGui, StringComparison.OrdinalIgnoreCase))
            {
                return new List<Result>();
            }

            try
            {
                // Fix #4: Check cache before scanning
                var cacheKey = $"{search}|{_maxResults}|{_maxDepth}|{_includeHiddenFiles}|{_showPercentage}";
                if (_scanCache.TryGetValue(cacheKey, out var cached) &&
                    DateTime.UtcNow - cached.Timestamp < CacheTtl)
                {
                    return cached.Results;
                }

                List<Result> results;

                // "largest <path>" â€” find largest files
                if (search.StartsWith(CmdLargest, StringComparison.OrdinalIgnoreCase))
                {
                    var path = search.Length > 8 ? search[8..].Trim().Trim('"') : string.Empty;
                    results = GetLargestFilesResults(path);
                }
                // "top <path>" â€” top subdirectories by size
                else if (search.StartsWith(CmdTop, StringComparison.OrdinalIgnoreCase))
                {
                    var path = search.Length > 4 ? search[4..].Trim().Trim('"') : string.Empty;
                    results = GetTopFoldersResults(path);
                }
                // "ext <path> <ext>" â€” find largest files by extension
                else if (search.StartsWith(CmdExt, StringComparison.OrdinalIgnoreCase))
                {
                    var argString = search.Length > 4 ? search[4..].Trim() : string.Empty;
                    var parts = argString.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var path = parts[0].Trim('"');
                        var ext = parts[1].Trim('"');
                        results = GetFilesByExtensionResults(path, ext);
                    }
                    else
                    {
                        return new List<Result> { new Result { Title = "Usage: ds ext <path> <extension>", SubTitle = "Example: ds ext C:\\ .mp4", IcoPath = _iconPath, Score = 100 } };
                    }
                }
                // "empty <path>" â€” find empty folders
                else if (search.StartsWith(CmdEmpty, StringComparison.OrdinalIgnoreCase))
                {
                    var path = search.Length > 6 ? search[6..].Trim().Trim('"') : string.Empty;
                    results = GetEmptyFoldersResults(path);
                }
                // Direct path â€” scan the directory
                else if (DiskAnalyzerHelper.IsValidPath(search))
                {
                    results = GetDirectoryScanResults(search.Trim('"'));
                }
                // Partial path or search term
                else
                {
                    return GetSuggestionResults(search); // Don't cache suggestions
                }

                // Store in cache and evict expired entries
                _scanCache[cacheKey] = (DateTime.UtcNow, results);
                foreach (var key in _scanCache.Keys.ToList())
                {
                    if (_scanCache.TryGetValue(key, out var entry) &&
                        DateTime.UtcNow - entry.Timestamp > CacheTtl)
                    {
                        _scanCache.TryRemove(key, out _);
                    }
                }

                return results;
            }
            catch (UnauthorizedAccessException)
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Access Denied",
                        SubTitle = $"Cannot access: {search}. Try running PowerToys as Administrator.",
                        IcoPath = _iconPath,
                        Score = 100,
                    },
                };
            }
            catch (Exception ex)
            {
                Log.Error($"DiskAnalyzer query error: {ex.Message}", GetType());
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Error analyzing path",
                        SubTitle = ex.Message,
                        IcoPath = _iconPath,
                        Score = 100,
                    },
                };
            }
        }

        /// <summary>
        /// Context menu actions for results.
        /// </summary>
        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            var menus = new List<ContextMenuResult>();

            if (selectedResult?.ContextData is not DiskItemInfo item)
            {
                return menus;
            }

            // Open in File Explorer
            menus.Add(new ContextMenuResult
            {
                PluginName = Name,
                Title = "Open in File Explorer (Ctrl+O)",
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                Glyph = "\xE838", // FolderOpen
                AcceleratorKey = Key.O,
                AcceleratorModifiers = ModifierKeys.Control,
                Action = _ =>
                {
                    try
                    {
                        var path = item.FullPath;
                        if (item.IsFile)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
                        }
                        else
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to open path: {ex.Message}", GetType());
                        return false;
                    }
                },
            });

            // Copy path
            menus.Add(new ContextMenuResult
            {
                PluginName = Name,
                Title = "Copy path (Ctrl+C)",
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                Glyph = "\xE8C8", // Copy
                AcceleratorKey = Key.C,
                AcceleratorModifiers = ModifierKeys.Control,
                Action = _ =>
                {
                    Clipboard.SetText(item.FullPath);
                    return true;
                },
            });

            // Copy size
            menus.Add(new ContextMenuResult
            {
                PluginName = Name,
                Title = $"Copy size: {DiskAnalyzerHelper.FormatSize(item.SizeBytes)} (Ctrl+Shift+C)",
                FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                Glyph = "\xE8EF", // ReportDocument
                AcceleratorKey = Key.C,
                AcceleratorModifiers = ModifierKeys.Control | ModifierKeys.Shift,
                Action = _ =>
                {
                    Clipboard.SetText(DiskAnalyzerHelper.FormatSize(item.SizeBytes));
                    return true;
                },
            });

            // Drill down (for folders)
            if (!item.IsFile)
            {
                // Fix #1: Changed from Ctrl+O (duplicate) to Ctrl+Enter
                menus.Add(new ContextMenuResult
                {
                    PluginName = Name,
                    Title = "Scan this folder (Ctrl+Enter)",
                    FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                    Glyph = "\xE721", // Search
                    AcceleratorKey = Key.Enter,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery($"ds {item.FullPath}", true);
                        return false;
                    },
                });

                // Find largest files in folder
                menus.Add(new ContextMenuResult
                {
                    PluginName = Name,
                    Title = "Find largest files here (Ctrl+L)",
                    FontFamily = "Segoe Fluent Icons,Segoe MDL2 Assets",
                    Glyph = "\xE74C", // SortLines
                    AcceleratorKey = Key.L,
                    AcceleratorModifiers = ModifierKeys.Control,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery($"ds largest {item.FullPath}", true);
                        return false;
                    },
                });
            }

            return menus;
        }

        #region Settings
        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>
        {
            new PluginAdditionalOption
            {
                Key = "MaxResults",
                DisplayLabel = "Maximum results",
                DisplayDescription = "Maximum number of items to display (5-50)",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = _maxResults,
                NumberBoxMin = 5,
                NumberBoxMax = 50,
            },
            new PluginAdditionalOption
            {
                Key = "MaxDepth",
                DisplayLabel = "Default scan depth",
                DisplayDescription = "How many levels deep to scan by default (1-5)",
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Numberbox,
                NumberValue = _maxDepth,
                NumberBoxMin = 1,
                NumberBoxMax = 5,
            },
            new PluginAdditionalOption
            {
                Key = "IncludeHidden",
                DisplayLabel = "Include hidden files and folders",
                DisplayDescription = "Include items with the Hidden attribute in results",
                Value = _includeHiddenFiles,
            },
            new PluginAdditionalOption
            {
                Key = "ShowPercentage",
                DisplayLabel = "Show percentage of parent",
                DisplayDescription = "Display what percentage of the parent folder each item uses",
                Value = _showPercentage,
            },
        };

        public System.Windows.Controls.Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            if (settings?.AdditionalOptions == null)
            {
                return;
            }

            // Clear cache when settings change
            _scanCache.Clear();

            foreach (var option in settings.AdditionalOptions)
            {
                switch (option.Key)
                {
                    case "MaxResults":
                        _maxResults = (int)option.NumberValue;
                        break;
                    case "MaxDepth":
                        _maxDepth = (int)option.NumberValue;
                        break;
                    case "IncludeHidden":
                        _includeHiddenFiles = option.Value;
                        break;
                    case "ShowPercentage":
                        _showPercentage = option.Value;
                        break;
                }
            }
        }
        #endregion

        #region Result Builders
        private List<Result> GetHelpResults()
        {
            return new List<Result>
            {
                new Result
                {
                    Title = "ValleySoft Disk Analyzer (Runs Plug-in) \u2014 TreeSize-like disk usage tool",
                    SubTitle = "Type a path, 'drives', 'largest <path>', or 'top <path>'",
                    IcoPath = _iconPath,
                    Score = 1000,
                },
                new Result
                {
                    Title = "drives",
                    SubTitle = "Show all drives with used/free/total space",
                    IcoPath = _iconPath,
                    Score = 900,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery("ds drives", true);
                        return false;
                    },
                },
                new Result
                {
                    Title = @"C:\Users",
                    SubTitle = "Scan a folder \u2014 shows subfolders sorted by size",
                    IcoPath = _iconPath,
                    Score = 800,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery(@"ds C:\Users", true);
                        return false;
                    },
                },
                new Result
                {
                    Title = "gui",
                    SubTitle = "Open the full standalone WPF graphical interface",
                    IcoPath = _iconPath,
                    Score = 750,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery("ds gui", true);
                        return false;
                    },
                },
                new Result
                {
                    Title = @"largest C:\",
                    SubTitle = "Find the largest files in a directory (recursive)",
                    IcoPath = _iconPath,
                    Score = 700,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery(@"ds largest C:\", true);
                        return false;
                    },
                },
                new Result
                {
                    Title = @"top C:\",
                    SubTitle = "Show top-level folders ranked by total size",
                    IcoPath = _iconPath,
                    Score = 600,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery(@"ds top C:\", true);
                        return false;
                    },
                },
                new Result
                {
                    Title = @"ext C:\ .mp4",
                    SubTitle = "Find largest files of a specific extension",
                    IcoPath = _iconPath,
                    Score = 500,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery(@"ds ext C:\ .mp4", true);
                        return false;
                    },
                },
                new Result
                {
                    Title = @"empty C:\",
                    SubTitle = "Find folders that contain no files or subfolders",
                    IcoPath = _iconPath,
                    Score = 400,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery(@"ds empty C:\", true);
                        return false;
                    },
                },
            };
        }

        private List<Result> GetDriveResults()
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady)
                .OrderByDescending(d => d.TotalSize - d.AvailableFreeSpace)
                .ToList();

            var results = new List<Result>();

            foreach (var drive in drives)
            {
                var usedBytes = drive.TotalSize - drive.AvailableFreeSpace;
                var allocatedBytes = usedBytes; // Drives: used bytes = allocated bytes (no P/Invoke on directory path)
                var usedPercent = (double)usedBytes / drive.TotalSize * 100;
                var bar = DiskAnalyzerHelper.CreateProgressBar(usedPercent);

                results.Add(new Result
                {
                    Title = $"{drive.Name} \u2014 {DiskAnalyzerHelper.FormatSize(usedBytes)} used of {DiskAnalyzerHelper.FormatSize(drive.TotalSize)} ({usedPercent:F1}%)",
                    SubTitle = $"{bar} Free: {DiskAnalyzerHelper.FormatSize(drive.TotalFreeSpace)} | Allocated: {DiskAnalyzerHelper.FormatSize(allocatedBytes)}",
                    IcoPath = _iconPath,
                    Score = (int)usedPercent,
                    ToolTipData = new ToolTipData(
                        $"Drive {drive.Name}",
                        $"Label: {drive.VolumeLabel}\n" +
                        $"Type: {drive.DriveType}\n" +
                        $"Format: {drive.DriveFormat}\n" +
                        $"Total: {DiskAnalyzerHelper.FormatSize(drive.TotalSize)}\n" +
                        $"Used: {DiskAnalyzerHelper.FormatSize(usedBytes)} ({usedPercent:F1}%)\n" +
                        $"Allocated: {DiskAnalyzerHelper.FormatSize(allocatedBytes)}\n" +
                        $"Free: {DiskAnalyzerHelper.FormatSize(drive.TotalFreeSpace)}"),
                    ContextData = new DiskItemInfo
                    {
                        FullPath = drive.Name,
                        SizeBytes = usedBytes,
                        AllocatedSizeBytes = allocatedBytes,
                        IsFile = false,
                    },
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery($"ds {drive.Name}", true);
                        return false;
                    },
                });
            }

            return results;
        }

        private List<Result> GetDirectoryScanResults(string path)
        {
            if (!Directory.Exists(path))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Directory not found",
                        SubTitle = $"Path does not exist: {path}",
                        IcoPath = _iconPath,
                        Score = 100,
                    },
                };
            }

            var items = DiskAnalyzerHelper.ScanDirectory(path, _maxDepth, _includeHiddenFiles);
            var parentSize = items.Sum(i => i.SizeBytes);
            var parentAllocated = items.Sum(i => i.AllocatedSizeBytes);

            var results = new List<Result>();
            
            var parentDir = Directory.GetParent(path);
            if (parentDir != null)
            {
                results.Add(new Result
                {
                    Title = $"⬅ Up one level to {parentDir.Name}",
                    SubTitle = $"Return to {parentDir.FullName}",
                    IcoPath = _iconPath,
                    Score = 10001,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery($"ds {parentDir.FullName}", true);
                        return false;
                    }
                });
            }

            results.Add(
                new Result
                {
                    Title = $"\U0001F4C1 {path} \u2014 Total: {DiskAnalyzerHelper.FormatSize(parentSize)} (Allocated: {DiskAnalyzerHelper.FormatSize(parentAllocated)})",
                    SubTitle = $"{items.Count(i => !i.IsFile)} folders, {items.Count(i => i.IsFile)} files scanned" +
                               (!_includeHiddenFiles ? " \u26A0\uFE0F Hidden files excluded \u2014 enable in Settings for accurate totals" : ""),
                    IcoPath = _iconPath,
                    Score = 10000,
                    ContextData = new DiskItemInfo
                    {
                        FullPath = path,
                        SizeBytes = parentSize,
                        AllocatedSizeBytes = parentAllocated,
                        IsFile = false,
                    },
                    Action = _ =>
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
                        return true;
                    },
                }
            );

            var sorted = items
                .OrderByDescending(i => i.SizeBytes)
                .Take(_maxResults);

            int rank = 1;
            foreach (var item in sorted)
            {
                var icon = item.IsFile ? "\U0001F4C4" : "\U0001F4C1";
                var pct = parentSize > 0 ? (double)item.SizeBytes / parentSize * 100 : 0;
                var pctText = _showPercentage ? $" ({pct:F1}%)" : string.Empty;
                var bar = DiskAnalyzerHelper.CreateMiniBar(pct);
                var hint = item.IsFile ? "" : " (Press Enter to drill down)";

                results.Add(new Result
                {
                    Title = $"{icon} {item.Name} \u2014 {DiskAnalyzerHelper.FormatSize(item.SizeBytes)}{pctText}",
                    SubTitle = $"{bar} Allocated: {DiskAnalyzerHelper.FormatSize(item.AllocatedSizeBytes)} | {item.FullPath}{hint}",
                    IcoPath = _iconPath,
                    Score = 10000 - rank,
                    ContextData = item,
                    Action = _ =>
                    {
                        if (item.IsFile)
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{item.FullPath}\"");
                        }
                        else
                        {
                            _context?.API.ChangeQuery($"ds {item.FullPath}", true);
                            return false;
                        }
                        return true;
                    },
                });
                rank++;
            }

            return results;
        }

        private List<Result> GetLargestFilesResults(string path)
        {
            if (!Directory.Exists(path))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Directory not found",
                        SubTitle = $"Path does not exist: {path}",
                        IcoPath = _iconPath,
                        Score = 100,
                    },
                };
            }

            var files = DiskAnalyzerHelper.FindLargestFiles(path, _maxResults, _includeHiddenFiles);

            if (!files.Any())
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "No files found",
                        SubTitle = $"No accessible files in: {path}",
                        IcoPath = _iconPath,
                        Score = 100,
                    },
                };
            }

            var results = new List<Result>
            {
                new Result
                {
                    Title = $"\U0001F50D Largest files in: {path}",
                    SubTitle = $"Showing top {Math.Min(files.Count, _maxResults)} files by size (recursive scan)",
                    IcoPath = _iconPath,
                    Score = 10000,
                },
            };

            int rank = 1;
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.Name).ToUpperInvariant();
                if (string.IsNullOrEmpty(ext)) ext = "(no ext)";

                results.Add(new Result
                {
                    Title = $"#{rank} {file.Name} \u2014 {DiskAnalyzerHelper.FormatSize(file.SizeBytes)}",
                    SubTitle = $"{ext} | Allocated: {DiskAnalyzerHelper.FormatSize(file.AllocatedSizeBytes)} | {file.FullPath}",
                    IcoPath = _iconPath,
                    Score = 10000 - rank,
                    ContextData = file,
                    ToolTipData = new ToolTipData(
                        file.Name,
                        $"Size: {DiskAnalyzerHelper.FormatSize(file.SizeBytes)} ({file.SizeBytes:N0} bytes)\n" +
                        $"Allocated: {DiskAnalyzerHelper.FormatSize(file.AllocatedSizeBytes)} ({file.AllocatedSizeBytes:N0} bytes)\n" +
                        $"Path: {file.FullPath}\n" +
                        $"Extension: {ext}\n" +
                        $"Modified: {file.LastModified:g}"),
                    Action = _ =>
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file.FullPath}\"");
                        return true;
                    },
                });
                rank++;
            }

            return results;
        }

        private List<Result> GetTopFoldersResults(string path)
        {
            if (!Directory.Exists(path))
            {
                return new List<Result>
                {
                    new Result
                    {
                        Title = "Directory not found",
                        SubTitle = $"Path does not exist: {path}",
                        IcoPath = _iconPath,
                        Score = 100,
                    },
                };
            }

            // Fix #3: Pass _maxDepth instead of hardcoded 10
            var folders = DiskAnalyzerHelper.GetTopFolders(path, _maxResults, _maxDepth, _includeHiddenFiles);
            var totalSize = folders.Sum(f => f.SizeBytes);
            var totalAllocated = folders.Sum(f => f.AllocatedSizeBytes);

            var results = new List<Result>
            {
                new Result
                {
                    Title = $"\U0001F4CA Top folders in: {path} \u2014 Total: {DiskAnalyzerHelper.FormatSize(totalSize)} (Allocated: {DiskAnalyzerHelper.FormatSize(totalAllocated)})",
                    SubTitle = $"{folders.Count} top-level subfolders scanned" +
                               (!_includeHiddenFiles ? " \u26A0\uFE0F Hidden files excluded" : ""),
                    IcoPath = _iconPath,
                    Score = 10000,
                    ContextData = new DiskItemInfo
                    {
                        FullPath = path,
                        SizeBytes = totalSize,
                        AllocatedSizeBytes = totalAllocated,
                        IsFile = false,
                    },
                },
            };

            int rank = 1;
            foreach (var folder in folders)
            {
                var pct = totalSize > 0 ? (double)folder.SizeBytes / totalSize * 100 : 0;
                var bar = DiskAnalyzerHelper.CreateMiniBar(pct);

                results.Add(new Result
                {
                    Title = $"\U0001F4C1 {folder.Name} \u2014 {DiskAnalyzerHelper.FormatSize(folder.SizeBytes)} ({pct:F1}%)",
                    SubTitle = $"{bar} Allocated: {DiskAnalyzerHelper.FormatSize(folder.AllocatedSizeBytes)} | Items: {folder.FileCount + folder.FolderCount} | {folder.FullPath} (Press Enter to drill down)",
                    IcoPath = _iconPath,
                    Score = 10000 - rank,
                    ContextData = folder,
                    Action = _ =>
                    {
                        _context?.API.ChangeQuery($"ds {folder.FullPath}", true);
                        return false;
                    },
                });
                rank++;
            }

            return results;
        }

        private List<Result> GetFilesByExtensionResults(string path, string ext)
        {
            if (!Directory.Exists(path))
            {
                return new List<Result>
                {
                    new Result { Title = "Directory not found", SubTitle = $"Path does not exist: {path}", IcoPath = _iconPath, Score = 100 }
                };
            }

            var files = DiskAnalyzerHelper.FindFilesByExtension(path, ext, _maxResults, _includeHiddenFiles);

            if (!files.Any())
            {
                return new List<Result>
                {
                    new Result { Title = "No files found", SubTitle = $"No accessible files ending in '{ext}' in: {path}", IcoPath = _iconPath, Score = 100 }
                };
            }

            var results = new List<Result>
            {
                new Result { Title = $"\U0001F50D Largest {ext} files in: {path}", SubTitle = $"Showing top {Math.Min(files.Count, _maxResults)} files", IcoPath = _iconPath, Score = 10000 }
            };

            int rank = 1;
            foreach (var file in files)
            {
                results.Add(new Result
                {
                    Title = $"#{rank} {file.Name} \u2014 {DiskAnalyzerHelper.FormatSize(file.SizeBytes)}",
                    SubTitle = $"Allocated: {DiskAnalyzerHelper.FormatSize(file.AllocatedSizeBytes)} | {file.FullPath}",
                    IcoPath = _iconPath,
                    Score = 10000 - rank,
                    ContextData = file,
                    Action = _ =>
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{file.FullPath}\"");
                        return true;
                    },
                });
                rank++;
            }

            return results;
        }

        private List<Result> GetEmptyFoldersResults(string path)
        {
            if (!Directory.Exists(path))
            {
                return new List<Result>
                {
                    new Result { Title = "Directory not found", SubTitle = $"Path does not exist: {path}", IcoPath = _iconPath, Score = 100 }
                };
            }

            var folders = DiskAnalyzerHelper.FindEmptyFolders(path, _maxResults, _includeHiddenFiles);

            if (!folders.Any())
            {
                return new List<Result>
                {
                    new Result { Title = "No empty folders found", SubTitle = $"Could not find any empty folders in: {path}", IcoPath = _iconPath, Score = 100 }
                };
            }

            var results = new List<Result>
            {
                new Result { Title = $"\U0001F4C1 Empty folders in: {path}", SubTitle = $"Showing up to {_maxResults} empty folders", IcoPath = _iconPath, Score = 10000 }
            };

            int rank = 1;
            foreach (var folder in folders)
            {
                results.Add(new Result
                {
                    Title = $"\U0001F4C1 {folder.Name}",
                    SubTitle = folder.FullPath,
                    IcoPath = _iconPath,
                    Score = 10000 - rank,
                    ContextData = folder,
                    Action = _ =>
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"\"{folder.FullPath}\"");
                        return true;
                    },
                });
                rank++;
            }

            return results;
        }

        private List<Result> GetSuggestionResults(string search)
        {
            var results = new List<Result>();

            try
            {
                var dir = Path.GetDirectoryName(search);
                var pattern = Path.GetFileName(search);

                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                {
                    // Fix #2: EnumerateDirectories instead of GetDirectories
                    var matches = Directory.EnumerateDirectories(dir, $"{pattern}*")
                        .Take(10)
                        .Select((d, i) => new Result
                        {
                            Title = $"\U0001F4C1 {Path.GetFileName(d)}",
                            SubTitle = d,
                            IcoPath = _iconPath,
                            Score = 1000 - i,
                            Action = _ =>
                            {
                                _context?.API.ChangeQuery($"ds {d}", true);
                                return false;
                            },
                        });
                    results.AddRange(matches);
                }
            }
            catch
            {
                // Ignore path parsing errors
            }

            if (!results.Any())
            {
                results.Add(new Result
                {
                    Title = $"No results for: {search}",
                    SubTitle = "Try a valid path like C:\\Users, or commands: drives, largest <path>, top <path>",
                    IcoPath = _iconPath,
                    Score = 100,
                });
            }

            return results;
        }
        #endregion

        #region Theme
        private void OnThemeChanged(Theme currentTheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                _iconPath = "Images/DiskAnalyzerLight.png";
            }
            else
            {
                _iconPath = "Images/DiskAnalyzerDark.png";
            }
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_context?.API != null)
                {
                    _context.API.ThemeChanged -= OnThemeChanged;
                }
                _scanCache.Clear();
                _disposed = true;
            }
        }
    }
}






  