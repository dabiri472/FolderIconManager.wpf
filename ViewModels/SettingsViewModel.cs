using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FolderIconManager.WPF.Models;
using FolderIconManager.WPF.Services;
using Microsoft.Win32;

namespace FolderIconManager.WPF.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private SettingsData _originalSettings;

        [ObservableProperty]
        private SettingsData _currentSettings = new();

        public SettingsViewModel()
        {
            _settingsService = new SettingsService();
            _originalSettings = CloneSettings(_settingsService.CurrentSettings);
            CurrentSettings = CloneSettings(_settingsService.CurrentSettings);
        }

        [RelayCommand]
        private void SelectIconPath()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "انتخاب پوشه دانلود ایکون‌ها",
                InitialDirectory = CurrentSettings.IconDownloadPath
            };

            if (dialog.ShowDialog() == true)
            {
                CurrentSettings.IconDownloadPath = dialog.FolderName;
            }
        }

        [RelayCommand]
        private void OpenIconsFolder()
        {
            try
            {
                if (System.IO.Directory.Exists(CurrentSettings.IconDownloadPath))
                {
                    Process.Start("explorer.exe", CurrentSettings.IconDownloadPath);
                }
                else
                {
                    MessageBox.Show("پوشه انتخاب شده وجود ندارد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در باز کردن پوشه: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SaveSettings()
        {
            try
            {
                // به‌روزرسانی تنظیمات در سرویس
                _settingsService.UpdateIconDownloadPath(CurrentSettings.IconDownloadPath);
                _settingsService.UpdateAutoApplyIcons(CurrentSettings.AutoApplyIcons);
                _settingsService.UpdateBackupOriginal(CurrentSettings.BackupOriginal);
                _settingsService.UpdateShowNotifications(CurrentSettings.ShowNotifications);

                // اطمینان از وجود پوشه
                _settingsService.EnsureIconsDirectory();

                _originalSettings = CloneSettings(CurrentSettings);
                
                MessageBox.Show("تنظیمات با موفقیت ذخیره شد.", "موفقیت", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // بستن پنجره تنظیمات
                if (Application.Current.Windows.Count > 0)
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is Views.SettingsWindow)
                        {
                            window.DialogResult = true;
                            window.Close();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در ذخیره تنظیمات: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            // بازگرداندن تنظیمات به حالت اصلی
            CurrentSettings = CloneSettings(_originalSettings);
            
            // بستن پنجره تنظیمات
            if (Application.Current.Windows.Count > 0)
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.SettingsWindow)
                    {
                        window.DialogResult = false;
                        window.Close();
                        break;
                    }
                }
            }
        }

        [RelayCommand]
        private void ResetToDefaults()
        {
            var result = MessageBox.Show(
                "آیا از بازنشانی تنظیمات به حالت پیش‌فرض اطمینان دارید؟", 
                "تأیید", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CurrentSettings.IconDownloadPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                    "FolderIcons"
                );
                CurrentSettings.AutoApplyIcons = true;
                CurrentSettings.BackupOriginal = true;
                CurrentSettings.ShowNotifications = true;
            }
        }

        private SettingsData CloneSettings(SettingsData original)
        {
            return new SettingsData
            {
                IconDownloadPath = original.IconDownloadPath,
                AutoApplyIcons = original.AutoApplyIcons,
                BackupOriginal = original.BackupOriginal,
                ShowNotifications = original.ShowNotifications
            };
        }
    }
}
