using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;

namespace ValleySoft_DiskAnalyzer_App
{
    public static class IconUtilities
    {
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint ExtractIconEx(string szFileName, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private static readonly ConcurrentDictionary<string, ImageSource> _iconCache = new();

        public static async Task<ImageSource?> GetIconAsync(string path, bool isFolder, Microsoft.UI.Dispatching.DispatcherQueue? dispatcher = null)
        {
            try
            {
                string cacheKey;
                if (isFolder)
                {
                    if (path.Length <= 3)
                        cacheKey = $"[Drive:{path}]";
                    else
                        cacheKey = "[Folder]";
                }
                else
                {
                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    // .exe/.dll/.ico/.lnk have unique per-file icons — cache by full path
                    bool perFileIcon = ext == ".exe" || ext == ".dll" || ext == ".ico" || ext == ".lnk";
                    cacheKey = perFileIcon ? path : (string.IsNullOrEmpty(ext) ? "[File]" : ext);
                }

                if (_iconCache.TryGetValue(cacheKey, out var cachedIcon))
                {
                    return cachedIcon;
                }

                // Offload shell P/Invoke and GDI bitmap conversion to a background task
                byte[]? pngBytes = await Task.Run(() =>
                {
                    try
                    {
                        SHFILEINFO shfi = new SHFILEINFO();
                        uint flags = SHGFI_ICON | SHGFI_SMALLICON;
                        uint attributes = 0;

                        if (isFolder)
                        {
                            // For drives, always hit disk; for regular folders use generic icon
                            if (path.Length > 3)
                            {
                                flags |= SHGFI_USEFILEATTRIBUTES;
                                attributes = FILE_ATTRIBUTE_DIRECTORY;
                            }
                        }
                        else
                        {
                            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                            bool needsRealPath = ext == ".exe" || ext == ".dll" || ext == ".ico" || ext == ".lnk";
                            if (needsRealPath && System.IO.File.Exists(path))
                            {
                                try
                                {
                                    IntPtr[] largeIcon = new IntPtr[1];
                                    IntPtr[] smallIcon = new IntPtr[1];
                                    uint count = ExtractIconEx(path, 0, largeIcon, smallIcon, 1);
                                    if (count > 0 && (smallIcon[0] != IntPtr.Zero || largeIcon[0] != IntPtr.Zero))
                                    {
                                        shfi.hIcon = smallIcon[0] != IntPtr.Zero ? smallIcon[0] : largeIcon[0];
                                        if (smallIcon[0] != IntPtr.Zero && largeIcon[0] != IntPtr.Zero)
                                        {
                                            DestroyIcon(largeIcon[0]);
                                        }
                                    }
                                }
                                catch { }
                            }

                            if (shfi.hIcon == IntPtr.Zero)
                            {
                                if (!needsRealPath)
                                {
                                    flags |= SHGFI_USEFILEATTRIBUTES;
                                    attributes = FILE_ATTRIBUTE_NORMAL;
                                }
                            }
                        }

                        IntPtr result = IntPtr.Zero;

                        if (shfi.hIcon == IntPtr.Zero)
                        {
                            result = SHGetFileInfo(path, attributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
                        }

                        // Retry with attribute fallback if real-path query failed
                        if ((result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero) && (flags & SHGFI_USEFILEATTRIBUTES) == 0)
                        {
                            flags |= SHGFI_USEFILEATTRIBUTES;
                            attributes = isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                            result = SHGetFileInfo(path, attributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
                        }

                        if (shfi.hIcon != IntPtr.Zero)
                        {
                            try
                            {
                                using var icon = System.Drawing.Icon.FromHandle(shfi.hIcon);
                                using var bmp = icon.ToBitmap();
                                using var stream = new MemoryStream();
                                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                                return stream.ToArray();
                            }
                            finally
                            {
                                DestroyIcon(shfi.hIcon);
                            }
                        }
                    }
                    catch
                    {
                        // Fallback on background task failure
                    }
                    return null;
                });

                if (pngBytes != null && pngBytes.Length > 0)
                {
                    if (dispatcher != null)
                    {
                        var tcs = new TaskCompletionSource<ImageSource?>();
                        dispatcher.TryEnqueue(async () =>
                        {
                            try
                            {
                                using var stream = new MemoryStream(pngBytes);
                                var bitmapImage = new BitmapImage();
                                await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                                _iconCache.TryAdd(cacheKey, bitmapImage);
                                tcs.SetResult(bitmapImage);
                            }
                            catch
                            {
                                tcs.SetResult(null);
                            }
                        });
                        return await tcs.Task;
                    }
                    else
                    {
                        using var stream = new MemoryStream(pngBytes);
                        var bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                        _iconCache.TryAdd(cacheKey, bitmapImage);
                        return bitmapImage;
                    }
                }
            }
            catch
            {
                // Ignore exceptions
            }

            return null;
        }
    }
}
