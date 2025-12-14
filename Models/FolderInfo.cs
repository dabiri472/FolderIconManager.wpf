using System;

namespace FolderIconManager.WPF.Models
{
    public class FolderInfo
    {
        public string Path { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool HasIcon { get; set; }
        public string IconPath { get; set; } = string.Empty;
        
        // برای نمایش در ListView
        public string DisplayPath => Path;
        public string StatusText => HasIcon ? "دارد" : "ندارد";
        public string StatusIcon => HasIcon ? "✅" : "❌";
    }
}
