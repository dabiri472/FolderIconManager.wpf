using System;
using System.IO;

namespace FolderIconManager.WPF.Models
{
    public class DriveInfoModel
    {
        public string Name { get; set; } = string.Empty;
        public string VolumeLabel { get; set; } = string.Empty;
        public long TotalFreeSpace { get; set; }
        public long TotalSize { get; set; }
        public DriveType DriveType { get; set; }
        public bool IsReady { get; set; }
        
        // برای نمایش در ListView
        public string DisplayName => $"{Name} ({(string.IsNullOrEmpty(VolumeLabel) ? "بدون نام" : VolumeLabel)}) - {FormatSize(TotalFreeSpace)} آزاد";
        public string StatusIcon => IsReady ? "💾" : "❌";
        public string DriveTypeInfo => DriveType switch
        {
            DriveType.Fixed => "دیسک داخلی",
            DriveType.Removable => "دیسک قابل حمل",
            DriveType.Network => "دیسک شبکه",
            DriveType.CDRom => "دیسک نوری",
            _ => "ناشناخته"
        };

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
