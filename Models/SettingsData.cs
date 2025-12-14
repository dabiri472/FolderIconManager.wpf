namespace FolderIconManager.WPF.Models
{
    public class SettingsData
    {
        public string IconDownloadPath { get; set; } = string.Empty;
        public bool AutoApplyIcons { get; set; }
        public bool BackupOriginal { get; set; }
        public bool ShowNotifications { get; set; }
    }
}
