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

        public static async Task<ImageSource?> GetIconAsync(string path, bool isFolder)
        {
            try
            {
                // We use extension as cache key for files, and "[Folder]" or "[Drive:C]" for folders/drives to reduce cache size
                string cacheKey;
                if (isFolder)
                {
                    if (path.Length <= 3) // Drive root, e.g., "C:\"
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

                SHFILEINFO shfi = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES;
                uint attributes = isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;

                // For drives we want the actual drive icon, so we must not use FILE_ATTRIBUTE
                if (isFolder && path.Length <= 3)
                {
                    flags &= ~SHGFI_USEFILEATTRIBUTES;
                }

                IntPtr result = SHGetFileInfo(path, attributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

                if (result != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        var bitmapSource = await GetImageSourceFromHIconAsync(shfi.hIcon);
                        if (bitmapSource != null)
                        {
                            _iconCache.TryAdd(cacheKey, bitmapSource);
                            return bitmapSource;
                        }
                    }
                    finally
                    {
                        DestroyIcon(shfi.hIcon);
                    }
                }
            }
            catch
            {
                // Ignore exceptions
            }

            return null;
        }

        private static async Task<ImageSource?> GetImageSourceFromHIconAsync(IntPtr hIcon)
        {
            try
            {
                // We can use Win32 CreateIconIndirect or just let System.Drawing extract it
                // To avoid System.Drawing dependency in WinUI, we'll try to convert HICON manually
                // Since this is WinUI 3, we can actually use System.Drawing.Common because we are on .NET 8
                using var icon = System.Drawing.Icon.FromHandle(hIcon);
                using var bmp = icon.ToBitmap();
                
                using var stream = new MemoryStream();
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                return bitmapImage;
            }
            catch
            {
                return null;
            }
        }
    }
}
