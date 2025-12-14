using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FolderIconManager.WPF.Models;

namespace FolderIconManager.WPF.Services
{
    public class FolderScannerService
    {
        // بررسی آیا پوشه ایکون سفارشی دارد
        private bool HasCustomIcon(string folderPath)
        {
            try
            {
                string desktopIniPath = Path.Combine(folderPath, "desktop.ini");
                if (!File.Exists(desktopIniPath))
                    return false;

                string[] lines = File.ReadAllLines(desktopIniPath);
                return lines.Any(line => 
                    line.StartsWith("IconResource=", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("IconFile=", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        // اسکن پوشه‌ها به صورت بازگشتی
        public async Task<(List<FolderInfo> withIcons, List<FolderInfo> withoutIcons)> ScanFoldersAsync(string rootPath, IProgress<ScanProgress> progress = null)
        {
            return await Task.Run(() =>
            {
                var foldersWithIcons = new List<FolderInfo>();
                var foldersWithoutIcons = new List<FolderInfo>();
                
                // شمارش کل پوشه‌ها برای پیشرفت
                int totalFolders = CountFolders(rootPath);
                int processed = 0;
                
                ScanFolder(rootPath, ref processed, totalFolders, foldersWithIcons, foldersWithoutIcons, progress);
                
                return (foldersWithIcons, foldersWithoutIcons);
            });
        }

        // شمارش پوشه‌ها
        private int CountFolders(string path)
        {
            try
            {
                return Directory.GetDirectories(path, "*", SearchOption.AllDirectories).Length + 1;
            }
            catch
            {
                return 1;
            }
        }

        // اسکن بازگشتی پوشه‌ها
        private void ScanFolder(string path, ref int processed, int total, 
            List<FolderInfo> foldersWithIcons, List<FolderInfo> foldersWithoutIcons, 
            IProgress<ScanProgress> progress)
        {
            try
            {
                var folders = Directory.GetDirectories(path);
                foreach (var folder in folders)
                {
                    var folderInfo = new FolderInfo
                    {
                        Path = folder,
                        Name = Path.GetFileName(folder)
                    };

                    // نادیده گرفتن پوشه‌های ICON
                    if (folderInfo.Name.Equals("ICON", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    folderInfo.HasIcon = HasCustomIcon(folder);

                    if (folderInfo.HasIcon)
                    {
                        foldersWithIcons.Add(folderInfo);
                    }
                    else
                    {
                        foldersWithoutIcons.Add(folderInfo);
                    }

                    processed++;
                    progress?.Report(new ScanProgress
                    {
                        Processed = processed,
                        Total = total,
                        CurrentFolder = folderInfo.Name,
                        Percentage = total > 0 ? (int)((double)processed / total * 100) : 0
                    });

                    ScanFolder(folder, ref processed, total, foldersWithIcons, foldersWithoutIcons, progress);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // نادیده گرفتن پوشه‌های بدون دسترسی
            }
            catch (Exception ex)
            {
                // لاگ خطا در نسخه نهایی
                System.Diagnostics.Debug.WriteLine($"Error scanning {path}: {ex.Message}");
            }
        }

        // اعمال ایکون به پوشه
        public async Task<bool> ApplyIconToFolderAsync(string folderPath, string iconPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // ایجاد desktop.ini
                    string desktopIniPath = Path.Combine(folderPath, "desktop.ini");
                    string desktopIniContent = $@"[.ShellClassInfo]
IconResource={iconPath},0

[ViewState]
Mode=
Vid=
FolderType=Generic
";

                    File.WriteAllText(desktopIniPath, desktopIniContent);

                    // تنظیم ویژگی‌های فایل‌ها
                    WindowsApiService.SetFileAttributes(desktopIniPath, 
                        WindowsApiService.FILE_ATTRIBUTE_SYSTEM | WindowsApiService.FILE_ATTRIBUTE_HIDDEN);
                    WindowsApiService.SetFileAttributes(folderPath, 
                        WindowsApiService.FILE_ATTRIBUTE_READONLY);

                    // رفرش Explorer
                    WindowsApiService.RefreshFolder(folderPath);

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        // حذف ایکون از پوشه
        public async Task<bool> RemoveIconFromFolderAsync(string folderPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    string desktopIniPath = Path.Combine(folderPath, "desktop.ini");
                    string iconFolder = Path.Combine(folderPath, "ICON");

                    // حذف desktop.ini
                    if (File.Exists(desktopIniPath))
                    {
                        File.SetAttributes(desktopIniPath, FileAttributes.Normal);
                        File.Delete(desktopIniPath);
                    }

                    // حذف پوشه ICON
                    if (Directory.Exists(iconFolder))
                    {
                        Directory.Delete(iconFolder, true);
                    }

                    // حذف ویژگی readonly از پوشه
                    WindowsApiService.SetFileAttributes(folderPath, 0);

                    // رفرش Explorer
                    WindowsApiService.RefreshFolder(folderPath);

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }
    }

    // کلاس برای گزارش پیشرفت اسکن
    public class ScanProgress
    {
        public int Processed { get; set; }
        public int Total { get; set; }
        public string CurrentFolder { get; set; } = string.Empty;
        public int Percentage { get; set; }
    }
}
