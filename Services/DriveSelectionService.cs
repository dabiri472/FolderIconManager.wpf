using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FolderIconManager.WPF.Models;

namespace FolderIconManager.WPF.Services
{
    public class DriveSelectionService
    {
        public List<DriveInfoModel> GetAvailableDrives()
        {
            try
            {
                return DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .Select(d => new DriveInfoModel
                    {
                        Name = d.Name,
                        VolumeLabel = d.VolumeLabel ?? "بدون نام",
                        TotalFreeSpace = d.TotalFreeSpace,
                        TotalSize = d.TotalSize,
                        DriveType = d.DriveType,
                        IsReady = d.IsReady
                    })
                    .OrderBy(d => d.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting drives: {ex.Message}");
                return new List<DriveInfoModel>();
            }
        }

        public List<DriveInfoModel> RefreshDrives()
        {
            // تازه‌سازی لیست درایوها (برای زمانی که درایوهای USB وصل می‌شوند)
            return GetAvailableDrives();
        }

        public bool IsDriveAccessible(string driveName)
        {
            try
            {
                var drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase));
                
                return drive?.IsReady == true && drive.DriveType == DriveType.Fixed;
            }
            catch
            {
                return false;
            }
        }

        public string GetDriveDisplayName(string driveName)
        {
            try
            {
                var drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase));
                
                if (drive != null && drive.IsReady)
                {
                    var driveModel = new DriveInfoModel
                    {
                        Name = drive.Name,
                        VolumeLabel = drive.VolumeLabel ?? "بدون نام",
                        TotalFreeSpace = drive.TotalFreeSpace,
                        TotalSize = drive.TotalSize,
                        DriveType = drive.DriveType,
                        IsReady = drive.IsReady
                    };
                    
                    return driveModel.DisplayName;
                }
                
                return driveName;
            }
            catch
            {
                return driveName;
            }
        }
    }
}
