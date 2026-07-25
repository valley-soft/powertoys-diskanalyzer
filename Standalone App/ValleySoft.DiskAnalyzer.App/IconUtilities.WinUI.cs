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
                    cacheKey = Path.GetExtension(path).ToLowerInvariant();
                    if (string.IsNullOrEmpty(cacheKey))
                        cacheKey = "[File]";
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
                        uint flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
                        uint attributes = isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;

                        if (isFolder && path.Length <= 3)
                        {
                            flags &= ~SHGFI_USEFILEATTRIBUTES;
                        }

                        IntPtr result = SHGetFileInfo(path, attributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

                        // If querying disk/network root without attributes failed, retry with attributes fallback
                        if ((result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero) && (flags & SHGFI_USEFILEATTRIBUTES) == 0)
                        {
                            flags |= SHGFI_USEFILEATTRIBUTES;
                            result = SHGetFileInfo(path, attributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
                        }

                        if (result != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
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
