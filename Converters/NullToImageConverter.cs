using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FolderIconManager.WPF.Converters
{
    public class NullToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                return null;
            }

            string imagePath = value.ToString();
            
            if (!File.Exists(imagePath))
            {
                return null;
            }

            try
            {
                // استفاده از BitmapCacheOption.OnLoad برای آزاد کردن فایل بلافاصله
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // مهم: فایل را در حافظه بارگذاری می‌کند و قفل را آزاد می‌کند
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                bitmap.Freeze(); // برای thread-safety و بهینه‌سازی
                
                return bitmap;
            }
            catch
            {
                return null;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
