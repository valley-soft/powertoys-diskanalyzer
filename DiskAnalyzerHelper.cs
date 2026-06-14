// Copyright (c) Thet. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Text;
#if !CMD_PAL
using Wox.Plugin.Logger;
#endif

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer
{
    internal static class LogHelper
    {
        public static void Error(string message, Type type)
        {
#if !CMD_PAL
            Log.Error(message, type);
#else
            System.Diagnostics.Debug.WriteLine($"ERROR [{type.Name}]: {message}");
#endif
        }

        public static void Warn(string message, Type type)
        {
#if !CMD_PAL
            Log.Warn(message, type);
#else
            System.Diagnostics.Debug.WriteLine($"WARN [{type.Name}]: {message}");
#endif
        }

        public static void Debug(string message, Type type)
        {
#if !CMD_PAL
            Log.Debug(message, type);
#else
            System.Diagnostics.Debug.WriteLine($"DEBUG [{type.Name}]: {message}");
#endif
        }
    }

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
                            LogHelper.Warn($"Error calculating size for {sub.FullName}: {ex.Message}", typeof(DiskAnalyzerHelper));
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
                    LogHelper.Warn($"Error enumerating directories in {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
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
                        LogHelper.Debug($"Skipping file {file.Name}: {ex.Message}", typeof(DiskAnalyzerHelper));
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
                        LogHelper.Warn($"Error calculating size for {sub.FullName}: {ex.Message}", typeof(DiskAnalyzerHelper));
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
                
                var topFiles = new SortedSet<(long Size, long Allocated, string Path, string Name, DateTime Modified)>(
                    Comparer<(long Size, long Allocated, string Path, string Name, DateTime Modified)>.Create(
                        (a, b) =>
                        {
                            var cmp = a.Size.CompareTo(b.Size);
                            return cmp != 0 ? cmp : string.Compare(a.Path, b.Path, StringComparison.Ordinal);
                        }));

                object lockObj = new object();

                dirInfo.EnumerateFiles("*" + extension, options).AsParallel().ForAll(file =>
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
                        LogHelper.Debug($"Skipping file {file.Name}: {ex.Message}", typeof(DiskAnalyzerHelper));
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
                var dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists) return emptyFolders;

                var dirs = dirInfo.EnumerateDirectories("*", options);
                var results = new System.Collections.Concurrent.ConcurrentBag<DiskItemInfo>();

                dirs.AsParallel().ForAll(subDir =>
                {
                    try
                    {
                        // Check if it has any children at all
                        if (!subDir.EnumerateFileSystemInfos().Any())
                        {
                            results.Add(new DiskItemInfo
                            {
                                Name = subDir.Name,
                                FullPath = subDir.FullName,
                                SizeBytes = 0,
                                AllocatedSizeBytes = 0,
                                IsFile = false,
                                ItemCount = 0,
                                LastModified = subDir.LastWriteTime,
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Debug($"Skipping directory {subDir.Name}: {ex.Message}", typeof(DiskAnalyzerHelper));
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
        private static (long totalSize, long totalAllocated, int itemCount) CalculateDirectorySize(string path, int depth, bool includeHidden)
        {
            long totalSize = 0;
            long totalAllocated = 0;
            int itemCount = 0;

            try
            {
                var dirInfo = new DirectoryInfo(path);
                var q = new Queue<DirectoryInfo>();
                q.Enqueue(dirInfo);

                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    try
                    {
                        if ((cur.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint) continue;

                        try
                        {
                            foreach (var f in cur.GetFiles())
                            {
                                long size = f.Length;
                                totalSize += size;
                                totalAllocated += GetAllocatedSize(f.FullName, size);
                                itemCount++;
                            }

                            if (depth > 1) // If recurse is true
                            {
                                foreach (var d in cur.GetDirectories())
                                {
                                    if ((d.Attributes & FileAttributes.ReparsePoint) != FileAttributes.ReparsePoint)
                                    {
                                        q.Enqueue(d);
                                    }
                                }
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Fallback to robocopy backup mode to scan this restricted folder recursively
                            if (depth > 1)
                            {
                                int subFilesCount;
                                long subSize = GetDirectorySizeWithRobocopy(cur.FullName, out subFilesCount);
                                totalSize += subSize;
                                totalAllocated += subSize; // Approximate allocation as same as size
                                itemCount += subFilesCount;
                            }
                        }
                        catch { }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"Error calculating size for {path}: {ex.Message}", typeof(DiskAnalyzerHelper));
            }

            return (totalSize, totalAllocated, itemCount);
        }

        private static string EscapePathForCommandLine(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path.EndsWith("\\"))
            {
                return path + "\\";
            }
            return path;
        }

        private static long GetDirectorySizeWithRobocopy(string path, out int fileCount)
        {
            fileCount = 0;
            try
            {
                string escapedPath = EscapePathForCommandLine(path);
                string escapedTemp = EscapePathForCommandLine(System.IO.Path.GetTempPath());
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "robocopy.exe",
                    Arguments = $"\"{escapedPath}\" \"{escapedTemp}\" /L /S /NJH /BYTES /B /XJ",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    long bytes = 0;
                    int files = 0;

                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.Contains("Files") && trimmed.Contains(":"))
                        {
                            var parts = trimmed.Split(new[] { ' ', '\t', ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2 && int.TryParse(parts[1], out int f))
                            {
                                files = f;
                            }
                        }
                        else if (trimmed.Contains("Bytes") && trimmed.Contains(":"))
                        {
                            var parts = trimmed.Split(new[] { ' ', '\t', ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2 && long.TryParse(parts[1], out long b))
                            {
                                bytes = b;
                            }
                        }
                    }

                    fileCount = files;
                    return bytes;
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}


  