// Copyright (c) Thet. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer
{
    /// <summary>
    /// Helper methods for disk scanning, size formatting, and UI elements.
    /// </summary>
    public static class DiskAnalyzerHelper
    {
        private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB", "TB", "PB" };

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetCompressedFileSizeW", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern uint GetCompressedFileSize(string lpFileName, out uint lpFileSizeHigh);

        public static long GetAllocatedSize(string path, long actualSize)
        {
            try
            {
                uint high;
                uint low = GetCompressedFileSize(path, out high);
                int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (low == 0xFFFFFFFF && error != 0)
                {
                    // Fallback to cluster size estimation (4KB)
                    long clusterSize = 4096;
                    return (actualSize + clusterSize - 1) / clusterSize * clusterSize;
                }
                return ((long)high << 32) + low;
            }
            catch
            {
                long clusterSize = 4096;
                return (actualSize + clusterSize - 1) / clusterSize * clusterSize;
            }
        }

        // Fix #2: Shared EnumerationOptions factory — no more duplicated construction
        private static EnumerationOptions CreateOptions(bool includeHidden, bool recurse = false, int maxDepth = 1)
        {
            return new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = recurse,
                MaxRecursionDepth = maxDepth,
                AttributesToSkip = includeHidden
                    ? FileAttributes.System
                    : FileAttributes.Hidden | FileAttributes.System,
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

                if (string.IsNullOrWhiteSpace(path))
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
        public static List<DiskItemInfo> ScanDirectory(string path, int maxDepth, bool includeHidden)
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

                // Fix #2: EnumerateDirectories instead of GetDirectories — lazy, no full array load
                try
                {
                    var subDirs = dirInfo.EnumerateDirectories("*", options).ToArray();
                    var folderItems = new DiskItemInfo[subDirs.Length];

                    Parallel.For(0, subDirs.Length, i =>
                    {
                        var sub = subDirs[i];
                        try
                        {
                            var (size, allocated, count) = CalculateDirectorySize(sub.FullName, int.MaxValue, includeHidden);
                            folderItems[i] = new DiskItemInfo
                            {
                                Name = sub.Name,
                                FullPath = sub.FullName,
                                SizeBytes = size,
                                AllocatedSizeBytes = allocated,
                                IsFile = false,
                                ItemCount = count,
                                LastModified = sub.LastWriteTime,
                            };
                        }
                        catch (Exception ex)
                        {
                            Log.Warn($"Error calculating size for {sub.FullName}: {ex.Message}", typeof(DiskAnalyzerHelper));
                            folderItems[i] = new DiskItemInfo
                            {
                                Name = sub.Name,
                                FullPath = sub.FullName,
                                SizeBytes = 0,
                                AllocatedSizeBytes = 0,
                                IsFile = false,
                                ItemCount = 0,
                                LastModified = sub.LastWriteTime,
                            };
                        }
                    });

                    items.AddRange(folderItems.Where(f => f != null));
                }
                catch (Exception ex)
                {
                    Log.Warn($"Error enumerating directories in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
                }

                // Fix #2: EnumerateFiles instead of GetFiles — lazy, no full array load
                try
                {
                    foreach (var file in dirInfo.EnumerateFiles("*", options))
                    {
                        try
                        {
                            items.Add(new DiskItemInfo
                            {
                                Name = file.Name,
                                FullPath = file.FullName,
                                SizeBytes = file.Length,
                                AllocatedSizeBytes = GetAllocatedSize(file.FullName, file.Length),
                                IsFile = true,
                                ItemCount = 1,
                                LastModified = file.LastWriteTime,
                            });
                        }
                        catch (Exception ex)
                        {
                            Log.Debug($"Skipping file {file.Name}: {ex.Message}", typeof(DiskAnalyzerHelper));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn($"Error enumerating files in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error scanning directory {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
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
                var dirInfo = new DirectoryInfo(path);
                var allFiles = dirInfo.EnumerateFiles("*", options);

                // Bounded SortedSet — only keeps top maxResults entries in memory
                var topFiles = new SortedSet<(long Size, long Allocated, string Path, string Name, DateTime Modified)>(
                    Comparer<(long Size, long Allocated, string Path, string Name, DateTime Modified)>.Create(
                        (a, b) =>
                        {
                            var cmp = a.Size.CompareTo(b.Size);
                            return cmp != 0 ? cmp : string.Compare(a.Path, b.Path, StringComparison.Ordinal);
                        }));

                object lockObj = new object();

                allFiles.AsParallel().ForAll(file =>
                {
                    try
                    {
                        long size = file.Length;
                        long allocated = GetAllocatedSize(file.FullName, size);
                        lock (lockObj)
                        {
                            topFiles.Add((size, allocated, file.FullName, file.Name, file.LastWriteTime));
                            if (topFiles.Count > maxResults)
                            {
                                topFiles.Remove(topFiles.Min);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"Skipping file {file.Name}: {ex.Message}", typeof(DiskAnalyzerHelper));
                    }
                });

                files = topFiles
                    .OrderByDescending(f => f.Size)
                    .Select(f => new DiskItemInfo
                    {
                        Name = f.Name,
                        FullPath = f.Path,
                        SizeBytes = f.Size,
                        AllocatedSizeBytes = f.Allocated,
                        IsFile = true,
                        ItemCount = 1,
                        LastModified = f.Modified,
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"Error finding largest files in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
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

                // Fix #2: EnumerateDirectories instead of GetDirectories
                var subDirs = dirInfo.EnumerateDirectories("*", options).ToArray();
                var folderItems = new DiskItemInfo[subDirs.Length];

                Parallel.For(0, subDirs.Length, i =>
                {
                    var sub = subDirs[i];
                    try
                    {
                        // Size must always be fully recursive regardless of display depth
                        var (size, allocated, count) = CalculateDirectorySize(sub.FullName, int.MaxValue, includeHidden);
                        folderItems[i] = new DiskItemInfo
                        {
                            Name = sub.Name,
                            FullPath = sub.FullName,
                            SizeBytes = size,
                            AllocatedSizeBytes = allocated,
                            IsFile = false,
                            ItemCount = count,
                            LastModified = sub.LastWriteTime,
                        };
                    }
                    catch (Exception ex)
                    {
                        Log.Warn($"Error calculating size for {sub.FullName}: {ex.Message}", typeof(DiskAnalyzerHelper));
                        folderItems[i] = new DiskItemInfo
                        {
                            Name = sub.Name,
                            FullPath = sub.FullName,
                            SizeBytes = 0,
                            AllocatedSizeBytes = 0,
                            IsFile = false,
                            ItemCount = 0,
                            LastModified = sub.LastWriteTime,
                        };
                    }
                });

                folders = folderItems
                    .Where(f => f != null)
                    .OrderByDescending(f => f.SizeBytes)
                    .Take(maxResults)
                    .ToList();
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting top folders in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            return folders;
        }

        /// <summary>
        /// Calculates total size and item count for a directory tree.
        /// </summary>
        private static (long totalSize, long totalAllocated, int itemCount) CalculateDirectorySize(string path, int depth, bool includeHidden)
        {
            long totalSize = 0;
            long totalAllocated = 0;
            int itemCount = 0;

            try
            {
                var options = CreateOptions(includeHidden, recurse: depth > 1, maxDepth: depth);
                var dirInfo = new DirectoryInfo(path);

                var files = dirInfo.EnumerateFiles("*", options).AsParallel();
                long localSize = 0;
                long localAllocated = 0;
                int localCount = 0;

                files.ForAll(file =>
                {
                    try
                    {
                        long size = file.Length;
                        Interlocked.Add(ref localSize, size);
                        Interlocked.Add(ref localAllocated, GetAllocatedSize(file.FullName, size));
                        Interlocked.Increment(ref localCount);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"Skipping file {file.Name}: {ex.Message}", typeof(DiskAnalyzerHelper));
                    }
                });

                totalSize = localSize;
                totalAllocated = localAllocated;
                itemCount = localCount;
            }
            catch (Exception ex)
            {
                Log.Debug($"Error calculating size for {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            return (totalSize, totalAllocated, itemCount);
        }
    }
}


  