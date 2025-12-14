using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FolderIconManager.WPF.Models;
using FolderIconManager.WPF.Services;

namespace FolderIconManager.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly FolderScannerService _folderScanner;
        private readonly DriveSelectionService _driveSelection;
        private readonly SettingsService _settings;

        [ObservableProperty]
        private ObservableCollection<DriveInfoModel> _availableDrives = new();

        [ObservableProperty]
        private ObservableCollection<DriveInfoModel> _selectedDrives = new();

        [ObservableProperty]
        private ObservableCollection<FolderInfo> _foldersWithIcons = new();

        [ObservableProperty]
        private ObservableCollection<FolderInfo> _foldersWithoutIcons = new();

        [ObservableProperty]
        private ObservableCollection<IconInfo> _availableIcons = new();

        [ObservableProperty]
        private IconInfo? _selectedIcon;

        partial void OnSelectedIconChanged(IconInfo? value)
        {
            ApplySelectedIconCommand?.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private string _statusMessage = "آماده برای اسکن پوشه‌ها";

        [ObservableProperty]
        private int _scanProgress;

        [ObservableProperty]
        private FolderInfo? _selectedFolderWithoutIcon;

        partial void OnSelectedFolderWithoutIconChanged(FolderInfo? value)
        {
            ApplySelectedIconCommand?.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private FolderInfo? _selectedFolderWithIcon;

        public MainViewModel()
        {
            _folderScanner = new FolderScannerService();
            _driveSelection = new DriveSelectionService();
            _settings = new SettingsService();
            
            LoadDrives();
            LoadAvailableIcons();
        }

        private void LoadDrives()
        {
            try
            {
                var drives = _driveSelection.GetAvailableDrives();
                AvailableDrives.Clear();
                foreach (var drive in drives)
                {
                    AvailableDrives.Add(drive);
                }
                
                StatusMessage = $"📁 مسیر دانلود: {_settings.CurrentSettings.IconDownloadPath} | 💾 {AvailableDrives.Count} درایو آماده";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطا در بارگذاری درایوها: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ScanFoldersAsync()
        {
            if (SelectedDrives.Count == 0)
            {
                StatusMessage = "⚠️ لطفاً یک یا چند درایو را برای اسکن انتخاب کنید";
                return;
            }

            IsScanning = true;
            StatusMessage = "در حال اسکن پوشه‌ها...";
            ScanProgress = 0;

            try
            {
                // پاک کردن لیست‌های قبلی
                FoldersWithIcons.Clear();
                FoldersWithoutIcons.Clear();

                int totalDrives = SelectedDrives.Count;
                int processedDrives = 0;

                foreach (var drive in SelectedDrives)
                {
                    StatusMessage = $"🔍 در حال اسکن درایو: {drive.Name} ({processedDrives + 1}/{totalDrives})";
                    
                    var progress = new Progress<ScanProgress>(p =>
                    {
                        // محاسبه درصد کلی (درایوها + پوشه‌ها)
                        int driveProgress = (int)((double)processedDrives / totalDrives * 100);
                        int folderProgress = (int)((double)p.Percentage / totalDrives);
                        ScanProgress = Math.Min(driveProgress + folderProgress, 99);
                        
                        StatusMessage = $"🔍 {drive.Name}: {p.CurrentFolder} ({p.Processed}/{p.Total})";
                    });

                    var (withIcons, withoutIcons) = await _folderScanner.ScanFoldersAsync(drive.Name, progress);

                    // اضافه کردن نتایج به ObservableCollection
                    foreach (var folder in withIcons)
                    {
                        FoldersWithIcons.Add(folder);
                    }

                    foreach (var folder in withoutIcons)
                    {
                        FoldersWithoutIcons.Add(folder);
                    }

                    processedDrives++;
                }

                StatusMessage = $"✅ اسکن کامل شد: {FoldersWithIcons.Count} پوشه با ایکون، {FoldersWithoutIcons.Count} پوشه بدون ایکون";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطا در اسکن: {ex.Message}";
            }
            finally
            {
                IsScanning = false;
                ScanProgress = 0;
            }
        }

        [RelayCommand]
        private void RefreshDrives()
        {
            LoadDrives();
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow();
            settingsWindow.Owner = Application.Current.MainWindow;
            settingsWindow.ShowDialog();
            
            // بارگذاری مجدد تنظیمات پس از بستن پنجره
            _settings.LoadSettings();
            StatusMessage = $"📁 مسیر دانلود: {_settings.CurrentSettings.IconDownloadPath} | ⚙️ آماده برای اسکن";
            
            // بارگذاری مجدد ایکون‌ها
            LoadAvailableIcons();
        }

        [RelayCommand]
        private void OpenIconsFolder()
        {
            try
            {
                if (System.IO.Directory.Exists(_settings.CurrentSettings.IconDownloadPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", _settings.CurrentSettings.IconDownloadPath);
                }
                else
                {
                    StatusMessage = "⚠️ پوشه ایکون‌ها وجود ندارد";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطا در باز کردن پوشه: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SearchIconsOnline()
        {
            try
            {
                string searchQuery = "folder icon";
                if (SelectedFolderWithoutIcon != null)
                {
                    searchQuery = $"{SelectedFolderWithoutIcon.Name} folder icon";
                }
                
                string url = $"https://images.google.com/search?q={Uri.EscapeDataString(searchQuery)}";
                System.Diagnostics.Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                
                StatusMessage = $"🌐 جستجوی ایکون برای: {searchQuery}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطا در باز کردن مرورگر: {ex.Message}";
            }
        }

        [RelayCommand]
        private void AddNewImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "انتخاب عکس برای ایکون",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.ico|All Files|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string fileName in dialog.FileNames)
                {
                    // کپی عکس به پوشه ایکون‌ها
                    string destPath = System.IO.Path.Combine(_settings.CurrentSettings.IconDownloadPath, System.IO.Path.GetFileName(fileName));
                    
                    if (!File.Exists(destPath))
                    {
                        File.Copy(fileName, destPath);
                    }
                }
                
                LoadAvailableIcons();
                StatusMessage = $"✅ {dialog.FileNames.Length} عکس با موفقیت اضافه شد";
            }
        }

        [RelayCommand]
        private void RefreshIcons()
        {
            LoadAvailableIcons();
            StatusMessage = "🔄 لیست ایکون‌ها بازخوانی شد";
        }

        private bool CanApplySelectedIcon()
        {
            return SelectedIcon != null && SelectedFolderWithoutIcon != null;
        }

        [RelayCommand(CanExecute = nameof(CanApplySelectedIcon))]
        private async Task ApplySelectedIconAsync()
        {
            if (SelectedIcon == null)
            {
                StatusMessage = "⚠️ لطفاً یک ایکون انتخاب کنید";
                return;
            }

            if (SelectedFolderWithoutIcon == null)
            {
                StatusMessage = "⚠️ لطفاً یک پوشه بدون ایکون انتخاب کنید";
                return;
            }

            try
            {
                StatusMessage = $"در حال اعمال ایکون {SelectedIcon.Name} به {SelectedFolderWithoutIcon.Name}...";

                // ایجاد پوشه ICON در پوشه اصلی
                string iconFolderPath = System.IO.Path.Combine(SelectedFolderWithoutIcon.Path, "ICON");
                StatusMessage = $"ایجاد پوشه: {iconFolderPath}";
                
                try
                {
                    if (!Directory.Exists(iconFolderPath))
                    {
                        Directory.CreateDirectory(iconFolderPath);
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ خطا در ایجاد پوشه ICON: {ex.Message}";
                    return;
                }

                // مسیر نهایی ایکون
                string finalIconPath = System.IO.Path.Combine(iconFolderPath, "icon.ico");
                StatusMessage = $"مسیر نهایی ایکون: {finalIconPath}";

                // تبدیل به ICO اگر لازم باشد
                StatusMessage = $"🔍 Debug: فایل انتخابی: {SelectedIcon.Path}, IsIconFile: {SelectedIcon.IsIconFile}";
                
                if (!SelectedIcon.IsIconFile)
                {
                    StatusMessage = $"تبدیل عکس به ICO: {SelectedIcon.Path} -> {finalIconPath}";
                    
                    try
                    {
                        IconConverterService.ConvertToIcon(SelectedIcon.Path, finalIconPath);
                        StatusMessage = "✅ تبدیل به ICO موفق بود";
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"❌ خطا در تبدیل به ICO: {ex.Message}";
                        return;
                    }
                    
                    // حذف عکس اصلی از پوشه ایکون‌ها
                    try
                    {
                        if (File.Exists(SelectedIcon.Path))
                        {
                            File.Delete(SelectedIcon.Path);
                            StatusMessage = "✅ عکس اصلی حذف شد";
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"❌ خطا در حذف عکس اصلی: {ex.Message}";
                        return;
                    }
                }
                else
                {
                    StatusMessage = $"استفاده مستقیم از فایل ICO: {SelectedIcon.Path}";
                    
                    try
                    {
                        // بررسی وجود فایل مقصد
                        if (File.Exists(finalIconPath))
                        {
                            File.Delete(finalIconPath);
                            StatusMessage = "فایل مقصد موجود حذف شد";
                        }
                        
                        // بررسی وجود فایل مبدا
                        if (!File.Exists(SelectedIcon.Path))
                        {
                            StatusMessage = $"❌ فایل مبدا وجود ندارد: {SelectedIcon.Path}";
                            return;
                        }
                        
                        // کپی فایل ICO به مقصد نهایی
                        File.Copy(SelectedIcon.Path, finalIconPath, true);
                        StatusMessage = "✅ فایل ICO با موفقیت کپی شد";
                        
                        // حذف فایل ICO اصلی از پوشه ایکون‌ها
                        try
                        {
                            if (File.Exists(SelectedIcon.Path))
                            {
                                File.Delete(SelectedIcon.Path);
                                StatusMessage = "✅ فایل ICO اصلی حذف شد";
                            }
                        }
                        catch (Exception ex)
                        {
                            StatusMessage = $"⚠️ فایل کپی شد ولی فایل اصلی قفل است: {ex.Message}";
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"❌ خطا در کپی فایل ICO: {ex.Message}";
                        return;
                    }
                }

                // اعمال ایکون به پوشه
                StatusMessage = $"اعمال ایکون به پوشه: {SelectedFolderWithoutIcon.Path}";
                
                try
                {
                    bool success = await _folderScanner.ApplyIconToFolderAsync(SelectedFolderWithoutIcon.Path, finalIconPath);

                    if (success)
                    {
                        // انتقال پوشه از لیست بدون ایکون به با ایکون
                        FoldersWithoutIcons.Remove(SelectedFolderWithoutIcon);
                        FoldersWithIcons.Add(SelectedFolderWithoutIcon);
                        
                        StatusMessage = $"✅ ایکون {SelectedIcon.Name} با موفقیت به {SelectedFolderWithoutIcon.Name} اعمال شد";
                        
                        // حذف ایکون از لیست ایکون‌های موجود
                        AvailableIcons.Remove(SelectedIcon);
                    }
                    else
                    {
                        StatusMessage = "❌ خطا در اعمال ایکون به پوشه (سرویس اسکنر خطا برگرداند)";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ خطا در اعمال ایکون به پوشه: {ex.Message}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ خطای عمومی: {ex.Message}\n\nStackTrace: {ex.StackTrace}";
            }
        }

        private void LoadAvailableIcons()
        {
            AvailableIcons.Clear();
            
            string iconsPath = _settings.CurrentSettings.IconDownloadPath;
            if (!Directory.Exists(iconsPath)) return;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".ico" };
            
            foreach (string file in Directory.GetFiles(iconsPath))
            {
                string extension = System.IO.Path.GetExtension(file).ToLower();
                if (allowedExtensions.Contains(extension))
                {
                    AvailableIcons.Add(new IconInfo(file));
                }
            }
        }

        
        [RelayCommand]
        private void ClearFoldersWithoutIcons()
        {
            FoldersWithoutIcons.Clear();
            StatusMessage = "🗑️ لیست پوشه‌های بدون ایکون خالی شد";
        }

        [RelayCommand]
        private void ClearFoldersWithIcons()
        {
            FoldersWithIcons.Clear();
            StatusMessage = "🔄 لیست پوشه‌های با ایکون بازنشانی شد";
        }

        [RelayCommand]
        private async Task ApplyIconToSelectedFolderAsync()
        {
            if (SelectedFolderWithoutIcon == null)
            {
                StatusMessage = "لطفاً یک پوشه بدون ایکون انتخاب کنید";
                return;
            }

            StatusMessage = $"در حال اعمال ایکون به {SelectedFolderWithoutIcon.Name}...";

            try
            {
                // استفاده از مسیر تنظیم شده برای ایکون‌ها
                string iconsPath = _settings.CurrentSettings.IconDownloadPath;
                string sampleImagePath = Path.Combine(iconsPath, "sample.png");
                
                if (!File.Exists(sampleImagePath))
                {
                    StatusMessage = $"لطفاً ابتدا یک تصویر نمونه در پوشه ایکون‌ها ایجاد کنید: {iconsPath}";
                    return;
                }

                string tempIconPath = Path.GetTempFileName();
                IconConverterService.ConvertToIcon(sampleImagePath, tempIconPath);

                bool success = await _folderScanner.ApplyIconToFolderAsync(SelectedFolderWithoutIcon.Path, tempIconPath);

                if (success)
                {
                    // انتقال از لیست بدون ایکون به لیست با ایکون
                    FoldersWithoutIcons.Remove(SelectedFolderWithoutIcon);
                    SelectedFolderWithoutIcon.HasIcon = true;
                    FoldersWithIcons.Add(SelectedFolderWithoutIcon);
                    
                    StatusMessage = $"ایکون با موفقیت به {SelectedFolderWithoutIcon.Name} اعمال شد";
                    
                    // حذف فایل موقت
                    IconConverterService.DeleteFileWithRetry(tempIconPath);
                }
                else
                {
                    StatusMessage = $"خطا در اعمال ایکون به {SelectedFolderWithoutIcon.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطا: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RemoveIconFromSelectedFolderAsync()
        {
            if (SelectedFolderWithIcon == null)
            {
                StatusMessage = "لطفاً یک پوشه با ایکون انتخاب کنید";
                return;
            }

            StatusMessage = $"در حال حذف ایکون از {SelectedFolderWithIcon.Name}...";

            try
            {
                bool success = await _folderScanner.RemoveIconFromFolderAsync(SelectedFolderWithIcon.Path);

                if (success)
                {
                    // انتقال از لیست با ایکون به لیست بدون ایکون
                    FoldersWithIcons.Remove(SelectedFolderWithIcon);
                    SelectedFolderWithIcon.HasIcon = false;
                    FoldersWithoutIcons.Add(SelectedFolderWithIcon);
                    
                    StatusMessage = $"ایکون با موفقیت از {SelectedFolderWithIcon.Name} حذف شد";
                }
                else
                {
                    StatusMessage = $"خطا در حذف ایکون از {SelectedFolderWithIcon.Name}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطا: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ClearLists()
        {
            FoldersWithIcons.Clear();
            FoldersWithoutIcons.Clear();
            StatusMessage = "لیست‌ها پاک شدند";
        }
    }
}
