// Copyright (c) Thet. All rights reserved.
// Licensed under the MIT License.

namespace Community.PowerToys.Run.Plugin.DiskAnalyzer
{
    public class DiskItemInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public long AllocatedSizeBytes { get; set; }
        public bool IsFile { get; set; }
        public int FileCount { get; set; }
        public int FolderCount { get; set; }
        public DateTime LastModified { get; set; }
    }
} 