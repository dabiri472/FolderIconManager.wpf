using System;
using System.Runtime.InteropServices;

namespace FolderIconManager.WPF.Services
{
    public static class WindowsApiService
    {
        // ثابت‌های ویژگی‌های فایل
        public const uint FILE_ATTRIBUTE_READONLY = 0x00000001;
        public const uint FILE_ATTRIBUTE_HIDDEN = 0x00000002;
        public const uint FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        
        // تنظیم ویژگی‌های فایل
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetFileAttributes(string lpFileName, uint dwFileAttributes);
        
        // رفرش Explorer
        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        
        // رفرش پوشه خاص
        public static void RefreshFolder(string path)
        {
            SHChangeNotify(0x02000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
        }
        
        // تنظیم ویژگی‌های پوشه
        public static bool SetFolderAttributes(string folderPath, uint attributes)
        {
            try
            {
                return SetFileAttributes(folderPath, attributes);
            }
            catch
            {
                return false;
            }
        }
        
        // بررسی آیا می‌توان پوشه را تغییر داد
        public static bool CanModifyFolder(string folderPath)
        {
            try
            {
                // تست نوشتن فایل موقت
                string testFile = System.IO.Path.Combine(folderPath, "test.tmp");
                System.IO.File.WriteAllText(testFile, "test");
                System.IO.File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
