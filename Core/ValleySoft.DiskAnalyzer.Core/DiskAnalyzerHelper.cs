// Copyright (c) Thet. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Text;
namespace Community.PowerToys.Run.Plugin.DiskAnalyzer
{
    internal static class LogHelper
    {
        public static void Error(string message, Type type)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR [{type.Name}]: {message}");
        }

        public static void Warn(string message, Type type)
        {
            System.Diagnostics.Debug.WriteLine($"WARN [{type.Name}]: {message}");
        }

        public static void Debug(string message, Type type)
        {
            System.Diagnostics.Debug.WriteLine($"DEBUG [{type.Name}]: {message}");
        }
    }

    /// <summary>
    /// Helper methods for disk scanning, size formatting, and UI elements.
    /// </summary>
    public static class DiskAnalyzerHelper
    {
        private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB", "TB", "PB" };
        private const FileAttributes RecallOnDataAccess = (FileAttributes)0x400000;

        // Fix 5: Cached EnumerationOptions — avoid allocating a new object on every scan call
        private static readonly EnumerationOptions _optsDefault = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            MaxRecursionDepth = int.MaxValue,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        };
        private static readonly EnumerationOptions _optsHidden = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = false,
            MaxRecursionDepth = int.MaxValue,
            AttributesToSkip = 0,
        };
        private static readonly EnumerationOptions _optsDefaultRecurse = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            MaxRecursionDepth = int.MaxValue,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System,
        };
        private static readonly EnumerationOptions _optsHiddenRecurse = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            MaxRecursionDepth = int.MaxValue,
            AttributesToSkip = 0,
        };

        // Fix 1: Cap parallelism to half core count — prevents disk I/O thrashing on SSDs/HDDs
        private static readonly ParallelOptions _parallelOpts = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount / 2)
        };

        // Fix 4: O(1) extension → category dictionary — replaces O(n×m) Array.Contains per file
        private static readonly Dictionary<string, string> _extToCategory = BuildExtensionMap();
        private static Dictionary<string, string> BuildExtensionMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (cat, exts) in new[]
            {
                ("Videos",           new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm" }),
                ("Audio",            new[] { ".mp3", ".wav", ".wma", ".ogg", ".flac", ".m4a", ".aac" }),
                ("Images",           new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".svg", ".ico", ".webp" }),
                ("Archives",         new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".iso", ".cab" }),
                ("Documents",        new[] { ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".txt", ".rtf", ".csv" }),
                ("Code",             new[] { ".cs", ".js", ".ts", ".html", ".css", ".json", ".xml", ".py", ".cpp", ".h", ".go", ".rs", ".java", ".sh", ".ps1", ".xaml" }),
                ("Apps/Executables", new[] { ".exe", ".msi", ".dll", ".sys", ".bat", ".cmd" }),
            })
            {
                foreach (var ext in exts)
                    map.TryAdd(ext, cat);
            }
            return map;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetCompressedFileSizeW", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern uint GetCompressedFileSize(string lpFileName, out uint lpFileSizeHigh);

        public static long GetAllocatedSize(string path, long actualSize, FileAttributes attributes = 0)
        {
            long clusterSize = 4096;
            
            // Cloud files and Reparse Points (OneDrive, iCloud, Symlinks) take 0 physical space.
            // Explicitly return 0 to avoid GetCompressedFileSizeW failing with Access Denied in MSIX.
            if ((attributes & (FileAttributes.Offline | FileAttributes.ReparsePoint | RecallOnDataAccess)) != 0)
            {
                return 0;
            }

            // Only P/Invoke if the file is compressed or sparse
            if (attributes == 0 || (attributes & (FileAttributes.Compressed | FileAttributes.SparseFile)) != 0)
            {
                try
                {
                    if (!path.StartsWith(@"\\?\") && path.Length >= 260)
                    {
                        path = @"\\?\" + path;
                    }

                    uint high;
                    uint low = GetCompressedFileSize(path, out high);
                    int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    if (low == 0xFFFFFFFF && error != 0)
                    {
                        return (actualSize + clusterSize - 1) / clusterSize * clusterSize;
                    }
                    return ((long)high << 32) + low;
                }
                catch
                {
                    return (actualSize + clusterSize - 1) / clusterSize * clusterSize;
                }
            }

            return (actualSize + clusterSize - 1) / clusterSize * clusterSize;
        }



        private static EnumerationOptions CreateOptions(bool includeHidden, bool recurse = false, int maxDepth = int.MaxValue)
        {
            // Fix 5: Return cached instances for common cases; fall back to allocation only for non-default maxDepth
            if (maxDepth == int.MaxValue)
            {
                if (!recurse) return includeHidden ? _optsHidden : _optsDefault;
                return includeHidden ? _optsHiddenRecurse : _optsDefaultRecurse;
            }
            // Non-standard depth (rare) — allocate
            return new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = recurse,
                MaxRecursionDepth = maxDepth,
                AttributesToSkip = includeHidden ? 0 : FileAttributes.Hidden | FileAttributes.System,
            };
        }

        /// <summary>
        /// Formats byte count into a human-readable string (e.g. "1.23 GB").
        /// </summary>
        public static string FormatSize(long bytes)
        {
            if (bytes < 0)
            {
                return "0 B";
            }

            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < SizeUnits.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return unitIndex == 0
                ? $"{size:F0} {SizeUnits[unitIndex]}"
                : $"{size:F2} {SizeUnits[unitIndex]}";
        }

        /// <summary>
        /// Creates a text-based progress bar for drive usage display.
        /// </summary>
        public static string CreateProgressBar(double percent, int width = 20)
        {
            var filled = (int)Math.Round(percent / 100 * width);
            filled = Math.Clamp(filled, 0, width);

            var bar = new StringBuilder();
            bar.Append('[');
            bar.Append('\u2588', filled);
            bar.Append('\u2591', width - filled);
            bar.Append(']');

            return bar.ToString();
        }

        /// <summary>
        /// Creates a compact progress bar for folder size display.
        /// </summary>
        public static string CreateMiniBar(double percent, int width = 10)
        {
            var filled = (int)Math.Round(percent / 100 * width);
            filled = Math.Clamp(filled, 0, width);

            var bar = new StringBuilder();
            bar.Append('\u2593', filled);
            bar.Append('\u2591', width - filled);

            return bar.ToString();
        }

        /// <summary>
        /// Validates whether a string is a valid file system path.
        /// </summary>
        public static bool IsValidPath(string path)
        {
            try
            {
                path = path.Trim().Trim('"');

                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
                {
                    return false;
                }

                // Canonicalize to resolve any ".." sequences
                path = Path.GetFullPath(path);

                // Must start with drive letter or UNC path
                if (!(path.Length >= 2 && path[1] == ':') && !path.StartsWith(@"\\"))
                {
                    return false;
                }

                return Path.IsPathRooted(path);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Scans a directory and returns items (files + folders) with their sizes.
        /// Uses parallel enumeration for performance.
        /// </summary>
        public static List<DiskItemInfo> ScanDirectory(string path, int maxDepth, bool includeHidden, IProgress<DiskItemInfo> progress = null)
        {
            var items = new List<DiskItemInfo>();

            try
            {
                var dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists)
                {
                    return items;
                }

                var options = CreateOptions(includeHidden, recurse: false);

                // Materialize subdirectories to array before parallel execution to prevent lazy enumeration race conditions
                try
                {
                    var subDirsList = new List<DirectoryInfo>();
                    try
                    {
                        using (var enumerator = dirInfo.EnumerateDirectories("*", options).GetEnumerator())
                        {
                            while (true)
                            {
                                try
                                {
                                    if (!enumerator.MoveNext()) break;
                                    if (enumerator.Current != null)
                                    {
                                        subDirsList.Add(enumerator.Current);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.Warn($"Skipping directory during enumeration in ScanDirectory: {ex.Message}", typeof(DiskAnalyzerHelper));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"Failed to initialize directory enumerator in ScanDirectory: {ex.Message}", typeof(DiskAnalyzerHelper));
                    }
                    var subDirs = subDirsList.ToArray();
                    var folderItems = new System.Collections.Concurrent.ConcurrentBag<DiskItemInfo>();

                    // Fix 1: Capped parallelism — prevents all cores hammering disk simultaneously
                    Parallel.ForEach(subDirs, _parallelOpts, sub =>
                    {
                        try
                        {
                            long size = 0;
                            long allocated = 0;
                            int fileCount = 0;
                            int folderCount = 0;

                            if ((sub.Attributes & FileAttributes.ReparsePoint) == 0)
                            {
                                (size, allocated, fileCount, folderCount) = CalculateDirectorySize(sub.FullName, int.MaxValue, includeHidden);
                            }

                            var item = new DiskItemInfo
                            {
                                Name = sub.Name,
                                FullPath = sub.FullName,
                                SizeBytes = size,
                                AllocatedSizeBytes = allocated,
                                IsFile = false,
                                FileCount = fileCount,
                                FolderCount = folderCount,
                                LastModified = sub.LastWriteTime,
                            };
                            folderItems.Add(item);
                            progress?.Report(item);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Warn($"Error calculating size for {sub.FullName}: {ex.Message}", typeof(DiskAnalyzerHelper));
                            var item = new DiskItemInfo
                            {
                                Name = sub.Name,
                                FullPath = sub.FullName,
                                SizeBytes = 0,
                                AllocatedSizeBytes = 0,
                                IsFile = false,
                                FileCount = 0,
                                FolderCount = 0,
                                LastModified = sub.LastWriteTime,
                            };
                            folderItems.Add(item);
                            progress?.Report(item);
                        }
                    });

                    items.AddRange(folderItems);
                }
                catch (Exception ex)
                {
                    LogHelper.Warn($"Error enumerating directories in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
                }

                // Enumerate files — lazy, no full array load
                try
                {
                    foreach (var file in dirInfo.EnumerateFiles("*", options))
                    {
                        try
                        {
                            // Fix 3: Skip P/Invoke for normal files — only call GetAllocatedSize for
                            // compressed/sparse files. Normal files use fast cluster-align math.
                            bool needsNativeSize = (file.Attributes &
                                (FileAttributes.Compressed | FileAttributes.SparseFile)) != 0 &&
                                (file.Attributes & (FileAttributes.Offline | FileAttributes.ReparsePoint | RecallOnDataAccess)) == 0;

                            long allocated = needsNativeSize
                                ? GetAllocatedSize(file.FullName, file.Length, file.Attributes)
                                : (file.Length + 4095L) / 4096L * 4096L;

                            var item = new DiskItemInfo
                            {
                                Name = file.Name,
                                FullPath = file.FullName,
                                SizeBytes = file.Length,
                                AllocatedSizeBytes = allocated,
                                IsFile = true,
                                FileCount = 1,
                                FolderCount = 0,
                                LastModified = file.LastWriteTime,
                            };
                            items.Add(item);
                            // NOTE: Files are NOT reported via progress — they are always present
                            // in the returned list. The UI reconciles files from the return value
                            // after scan completes. Only folders use progress for live streaming.
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Debug($"Skipping file {file.Name}: {ex.Message}", typeof(DiskAnalyzerHelper));
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Warn($"Error enumerating files in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error scanning directory {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            return items;
        }

        /// <summary>
        /// Recursively finds the largest files under a given path.
        /// Uses a bounded SortedSet to limit memory to maxResults entries.
        /// </summary>
        public static List<DiskItemInfo> FindLargestFiles(string path, int maxResults, bool includeHidden)
        {
            var files = new List<DiskItemInfo>();

            try
            {
                var options = CreateOptions(includeHidden, recurse: true, maxDepth: int.MaxValue);
                
                long minSize = 0;
                int currentCount = 0;

                var enumerable = new System.IO.Enumeration.FileSystemEnumerable<(long size, bool isDir, FileAttributes attrs, string fullPath, string name, DateTime modified)>(
                    path,
                    (ref System.IO.Enumeration.FileSystemEntry entry) => (entry.Length, entry.IsDirectory, entry.Attributes, entry.ToFullPath(), entry.FileName.ToString(), entry.LastWriteTimeUtc.LocalDateTime),
                    options)
                {
                    ShouldIncludePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => 
                    {
                        if (entry.IsDirectory) return false;
                        if (Interlocked.CompareExchange(ref currentCount, 0, 0) < maxResults) return true;
                        return entry.Length > Interlocked.Read(ref minSize);
                    },
                    ShouldRecursePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => (entry.Attributes & FileAttributes.ReparsePoint) == 0
                };

                // Bounded SortedSet — only keeps top maxResults entries in memory
                var topFiles = new SortedSet<(long Size, long Allocated, string Path, string Name, DateTime Modified)>(
                    Comparer<(long Size, long Allocated, string Path, string Name, DateTime Modified)>.Create(
                        (a, b) =>
                        {
                            var cmp = a.Size.CompareTo(b.Size);
                            return cmp != 0 ? cmp : string.Compare(a.Path, b.Path, StringComparison.Ordinal);
                        }));

                object lockObj = new object();

                foreach (var file in enumerable)
                {
                    try
                    {
                        long size = file.size;
                        long allocated = GetAllocatedSize(file.fullPath, size, file.attrs);
                        lock (lockObj)
                        {
                            topFiles.Add((size, allocated, file.fullPath, file.name, file.modified));
                            if (topFiles.Count > maxResults)
                            {
                                topFiles.Remove(topFiles.Min);
                            }
                            Interlocked.Exchange(ref currentCount, topFiles.Count);
                            Interlocked.Exchange(ref minSize, topFiles.Min.Size);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Debug($"Skipping file {file.name}: {ex.Message}", typeof(DiskAnalyzerHelper));
                    }
                }

                files = topFiles
                    .OrderByDescending(f => f.Size)
                    .Select(f => new DiskItemInfo
                    {
                        Name = f.Name,
                        FullPath = f.Path,
                        SizeBytes = f.Size,
                        AllocatedSizeBytes = f.Allocated,
                        IsFile = true,
                        FileCount = 1,
                        FolderCount = 0,
                        LastModified = f.Modified,
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error finding largest files in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            return files;
        }

        /// <summary>
        /// Gets top-level subdirectories ranked by total size.
        /// Fix #3: maxDepth is now passed in instead of hardcoded to 10.
        /// </summary>
        public static List<DiskItemInfo> GetTopFolders(string path, int maxResults, int maxDepth, bool includeHidden)
        {
            var folders = new List<DiskItemInfo>();

            try
            {
                var dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists)
                {
                    return folders;
                }

                var options = CreateOptions(includeHidden, recurse: false);

                // Materialize to array to bypass Enumerator lock contention in Parallel.ForEach
                var subDirsList = new List<DirectoryInfo>();
                try
                {
                    using (var enumerator = dirInfo.EnumerateDirectories("*", options).GetEnumerator())
                    {
                        while (true)
                        {
                            try
                            {
                                if (!enumerator.MoveNext()) break;
                                if (enumerator.Current != null)
                                {
                                    subDirsList.Add(enumerator.Current);
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Warn($"Skipping directory during enumeration in GetTopFolders: {ex.Message}", typeof(DiskAnalyzerHelper));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"Failed to initialize directory enumerator in GetTopFolders: {ex.Message}", typeof(DiskAnalyzerHelper));
                }
                var subDirs = subDirsList.ToArray();
                var folderItems = new System.Collections.Concurrent.ConcurrentBag<DiskItemInfo>();

                // Fix 1: Capped parallelism
                Parallel.ForEach(subDirs, _parallelOpts, sub =>
                {
                    try
                    {
                        long size = 0;
                        long allocated = 0;
                        int fileCount = 0;
                        int folderCount = 0;

                        if ((sub.Attributes & FileAttributes.ReparsePoint) == 0)
                        {
                            // Size must always be fully recursive regardless of display depth
                            (size, allocated, fileCount, folderCount) = CalculateDirectorySize(sub.FullName, int.MaxValue, includeHidden);
                        }

                        folderItems.Add(new DiskItemInfo
                        {
                            Name = sub.Name,
                            FullPath = sub.FullName,
                            SizeBytes = size,
                            AllocatedSizeBytes = allocated,
                            IsFile = false,
                            FileCount = fileCount,
                            FolderCount = folderCount,
                            LastModified = sub.LastWriteTime,
                        });
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Warn($"Error calculating size for {sub.FullName}: {ex.Message}", typeof(DiskAnalyzerHelper));
                        folderItems.Add(new DiskItemInfo
                        {
                            Name = sub.Name,
                            FullPath = sub.FullName,
                            SizeBytes = 0,
                            AllocatedSizeBytes = 0,
                            IsFile = false,
                            FileCount = 0,
                            FolderCount = 0,
                            LastModified = sub.LastWriteTime,
                        });
                    }
                });

                folders = folderItems
                    .OrderByDescending(f => f.SizeBytes)
                    .Take(maxResults)
                    .ToList();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error getting top folders in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            return folders;
        }

        /// <summary>
        /// Finds the largest files of a specific extension.
        /// </summary>
        public static List<DiskItemInfo> FindFilesByExtension(string path, string extension, int maxResults, bool includeHidden)
        {
            var files = new List<DiskItemInfo>();

            try
            {
                if (!extension.StartsWith(".")) extension = "." + extension;
                var options = CreateOptions(includeHidden, recurse: true, maxDepth: int.MaxValue);
                var dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists) return files;

                long minSize = 0;
                int currentCount = 0;

                var enumerable = new System.IO.Enumeration.FileSystemEnumerable<(long size, bool isDir, FileAttributes attrs, string fullPath, string name, DateTime modified)>(
                    path,
                    (ref System.IO.Enumeration.FileSystemEntry entry) => (entry.Length, entry.IsDirectory, entry.Attributes, entry.ToFullPath(), entry.FileName.ToString(), entry.LastWriteTimeUtc.LocalDateTime),
                    options)
                {
                    ShouldIncludePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => 
                    {
                        if (entry.IsDirectory || !entry.FileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) return false;
                        if (Interlocked.CompareExchange(ref currentCount, 0, 0) < maxResults) return true;
                        return entry.Length > Interlocked.Read(ref minSize);
                    },
                    ShouldRecursePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => (entry.Attributes & FileAttributes.ReparsePoint) == 0
                };
                
                var topFiles = new SortedSet<(long Size, long Allocated, string Path, string Name, DateTime Modified)>(
                    Comparer<(long Size, long Allocated, string Path, string Name, DateTime Modified)>.Create(
                        (a, b) =>
                        {
                            var cmp = a.Size.CompareTo(b.Size);
                            return cmp != 0 ? cmp : string.Compare(a.Path, b.Path, StringComparison.Ordinal);
                        }));

                object lockObj = new object();

                foreach (var file in enumerable)
                {
                    try
                    {
                        long size = file.size;
                        long allocated = GetAllocatedSize(file.fullPath, size, file.attrs);
                        lock (lockObj)
                        {
                            topFiles.Add((size, allocated, file.fullPath, file.name, file.modified));
                            if (topFiles.Count > maxResults)
                            {
                                topFiles.Remove(topFiles.Min);
                            }
                            Interlocked.Exchange(ref currentCount, topFiles.Count);
                            Interlocked.Exchange(ref minSize, topFiles.Min.Size);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Debug($"Skipping file {file.name}: {ex.Message}", typeof(DiskAnalyzerHelper));
                    }
                }

                files = topFiles
                    .OrderByDescending(f => f.Size)
                    .Select(f => new DiskItemInfo
                    {
                        Name = f.Name,
                        FullPath = f.Path,
                        SizeBytes = f.Size,
                        AllocatedSizeBytes = f.Allocated,
                        IsFile = true,
                        FileCount = 1,
                        FolderCount = 0,
                        LastModified = f.Modified,
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error finding files by extension {extension} in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            return files;
        }

        /// <summary>
        /// Finds folders that contain 0 files and 0 bytes.
        /// </summary>
        public static List<DiskItemInfo> FindEmptyFolders(string path, int maxResults, bool includeHidden)
        {
            var emptyFolders = new List<DiskItemInfo>();

            try
            {
                var options = CreateOptions(includeHidden, recurse: true, maxDepth: int.MaxValue);
                
                var enumerable = new System.IO.Enumeration.FileSystemEnumerable<(bool isDir, string fullPath, string name, DateTime modified)>(
                    path,
                    (ref System.IO.Enumeration.FileSystemEntry entry) => (entry.IsDirectory, entry.ToFullPath(), entry.FileName.ToString(), entry.LastWriteTimeUtc.LocalDateTime),
                    options)
                {
                    ShouldIncludePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => entry.IsDirectory,
                    ShouldRecursePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => (entry.Attributes & FileAttributes.ReparsePoint) == 0
                };

                var results = new System.Collections.Concurrent.ConcurrentBag<DiskItemInfo>();
                // Fix 6: Stream directories instead of .ToList() — avoids loading entire tree into memory
                // Fix 1: Capped parallelism
                var directoryItems = enumerable; // streaming — no ToList()
                int emptyCount = 0;

                Parallel.ForEach(directoryItems, _parallelOpts, (item, state) =>
                {
                    if (!item.isDir) return;
                    if (System.Threading.Volatile.Read(ref emptyCount) >= maxResults)
                    {
                        state.Stop();
                        return;
                    }

                    try
                    {
                        var subDir = new DirectoryInfo(item.fullPath);
                        if (!subDir.EnumerateFileSystemInfos().Any())
                        {
                            results.Add(new DiskItemInfo
                            {
                                Name = item.name,
                                FullPath = item.fullPath,
                                SizeBytes = 0,
                                AllocatedSizeBytes = 0,
                                IsFile = false,
                                FileCount = 0,
                                FolderCount = 0,
                                LastModified = item.modified,
                            });
                            System.Threading.Interlocked.Increment(ref emptyCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Debug($"Skipping directory {item.fullPath}: {ex.Message}", typeof(DiskAnalyzerHelper));
                    }
                });

                emptyFolders = results.Take(maxResults).ToList();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error finding empty folders in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            return emptyFolders;
        }

        /// <summary>
        /// Calculates total size and item count for a directory tree.
        /// </summary>
        private static (long totalSize, long totalAllocated, int fileCount, int folderCount) CalculateDirectorySize(string path, int depth, bool includeHidden)
        {
            long totalSize = 0;
            long totalAllocated = 0;
            int fileCount = 0;
            int folderCount = 0;
            long clusterSize = 4096;

            try
            {
                var options = CreateOptions(includeHidden, recurse: true, maxDepth: depth);
                
                var enumerable = new System.IO.Enumeration.FileSystemEnumerable<(long size, bool isDir, FileAttributes attrs, string fullPath)>(
                    path,
                    (ref System.IO.Enumeration.FileSystemEntry entry) => 
                    {
                        string p = null;
                        if (!entry.IsDirectory && (entry.Attributes & (FileAttributes.Compressed | FileAttributes.SparseFile)) != 0 && (entry.Attributes & FileAttributes.ReparsePoint) == 0)
                        {
                            p = entry.ToFullPath();
                        }
                        return (entry.Length, entry.IsDirectory, entry.Attributes, p);
                    },
                    options)
                {
                    ShouldIncludePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => true,
                    ShouldRecursePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => (entry.Attributes & FileAttributes.ReparsePoint) == 0
                };

                foreach (var (size, isDir, attrs, fullPath) in enumerable)
                {
                    if (isDir)
                    {
                        folderCount++;
                    }
                    else
                    {
                        totalSize += size;
                        if ((attrs & (FileAttributes.Offline | FileAttributes.ReparsePoint | FileAttributes.Compressed | FileAttributes.SparseFile | RecallOnDataAccess)) != 0)
                        {
                            totalAllocated += GetAllocatedSize(fullPath, size, attrs);
                        }
                        else
                        {
                            totalAllocated += (size + clusterSize - 1) / clusterSize * clusterSize;
                        }
                        fileCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"Error calculating size for {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            return (totalSize, totalAllocated, fileCount, folderCount);
        }


        public static List<(string Category, long Size, double Percentage)> GetFileTypeBreakdown(string path, bool includeHidden)
        {
            // Fix 4: Use O(1) dictionary lookup instead of O(n×m) Array.Contains per file
            var categoryOrder = new[] { "Videos", "Audio", "Images", "Archives", "Documents", "Code", "Apps/Executables" };
            var sizes = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var cat in categoryOrder) sizes[cat] = 0;
            long otherSize = 0;
            long totalSize = 0;

            try
            {
                var options = CreateOptions(includeHidden, recurse: true, maxDepth: int.MaxValue);
                var enumerable = new System.IO.Enumeration.FileSystemEnumerable<(long size, string name)>(
                    path,
                    (ref System.IO.Enumeration.FileSystemEntry entry) => (entry.Length, entry.FileName.ToString()),
                    options)
                {
                    ShouldIncludePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => !entry.IsDirectory,
                    ShouldRecursePredicate = (ref System.IO.Enumeration.FileSystemEntry entry) => (entry.Attributes & FileAttributes.ReparsePoint) == 0
                };

                foreach (var file in enumerable)
                {
                    try
                    {
                        long fileSize = file.size;
                        totalSize += fileSize;
                        string ext = System.IO.Path.GetExtension(file.name);
                        if (!string.IsNullOrEmpty(ext) && _extToCategory.TryGetValue(ext, out string catName))
                            sizes[catName] += fileSize;
                        else
                            otherSize += fileSize;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"Error calculating file type breakdown for {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            var result = new List<(string Category, long Size, double Percentage)>();
            foreach (var cat in categoryOrder)
            {
                long size = sizes[cat];
                double pct = totalSize > 0 ? (double)size / totalSize * 100 : 0;
                result.Add((cat, size, pct));
            }
            double otherPct = totalSize > 0 ? (double)otherSize / totalSize * 100 : 0;
            result.Add(("Other Files", otherSize, otherPct));

            return result.OrderByDescending(r => r.Size).ToList();
        }
    }
}


  