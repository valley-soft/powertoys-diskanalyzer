using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer
{
    public static class IconUtilities
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        private static readonly ConcurrentDictionary<string, ImageSource?> _iconCache = new(StringComparer.OrdinalIgnoreCase);

        public static ImageSource? GetIcon(string path, bool isFolder = false)
        {
            // Cache by extension for files, and by path for drives/folders
            string cacheKey = path;
            if (!isFolder && !string.IsNullOrEmpty(path))
            {
                cacheKey = Path.GetExtension(path);
                if (string.IsNullOrEmpty(cacheKey))
                    cacheKey = "FILE";
            }
            else if (isFolder && path.Length > 3)
            {
                // Generic folder cache to avoid querying every folder
                cacheKey = "FOLDER";
            }

            if (_iconCache.TryGetValue(cacheKey, out ImageSource? cachedIcon))
            {
                return cachedIcon;
            }

            uint flags = SHGFI_ICON | SHGFI_SMALLICON;
            uint attributes = 0;

            // For generic files/folders, use attributes so we don't hit the disk
            if (cacheKey == "FOLDER" || (!isFolder && cacheKey.StartsWith(".")))
            {
                flags |= SHGFI_USEFILEATTRIBUTES;
                attributes = isFolder ? 0x10u : 0x80u; // FILE_ATTRIBUTE_DIRECTORY or FILE_ATTRIBUTE_NORMAL
            }

            SHFILEINFO shfi = new SHFILEINFO();
            IntPtr res = SHGetFileInfo(path, attributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            if (res != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
            {
                try
                {
                    var bitmap = Imaging.CreateBitmapSourceFromHIcon(
                        shfi.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    
                    bitmap.Freeze();
                    _iconCache[cacheKey] = bitmap;
                    return bitmap;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    DestroyIcon(shfi.hIcon);
                }
            }

            return null;
        }
    }
}
