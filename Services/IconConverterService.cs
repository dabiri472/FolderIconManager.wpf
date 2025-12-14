using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace FolderIconManager.WPF.Services
{
    public class IconConverterService
    {
        // تبدیل تصویر به فرمت ICO چند رزولوشن
        public static string ConvertToIcon(string imagePath, string outputPath)
        {
            using (var sourceImage = Image.FromFile(imagePath))
            using (var iconStream = new FileStream(outputPath, FileMode.Create))
            {
                var sizes = new[] { 256, 128, 64, 48, 32, 16 };
                
                // نوشتن هدر ICO
                iconStream.WriteByte(0);  // Reserved
                iconStream.WriteByte(0);  // Reserved
                iconStream.WriteByte(1);  // Type (1 = ICO)
                iconStream.WriteByte(0);  // Type
                iconStream.WriteByte((byte)sizes.Length);  // Image count
                iconStream.WriteByte(0);  // Image count
                
                // محاسبه آفست‌ها
                int dataOffset = 6 + (sizes.Length * 16);
                var imageEntries = new (int offset, int size)[sizes.Length];
                
                // نوشتن directory entries
                for (int i = 0; i < sizes.Length; i++)
                {
                    int size = sizes[i];
                    
                    // محاسبه دقیق سایز: BMP header + pixel data + AND mask
                    var bmpHeaderSize = 40;
                    var pixelDataSize = size * size * 4; // 32 bits per pixel
                    var andMaskRowSize = ((size + 31) / 32) * 4; // 4-byte aligned rows
                    var andMaskSize = andMaskRowSize * size;
                    var totalSize = bmpHeaderSize + pixelDataSize + andMaskSize;
                    
                    imageEntries[i] = (dataOffset, totalSize);
                    dataOffset += totalSize;
                    
                    // نوشتن directory entry
                    iconStream.WriteByte((byte)size);  // Width
                    iconStream.WriteByte((byte)size);  // Height
                    iconStream.WriteByte(0);  // Color count
                    iconStream.WriteByte(0);  // Reserved
                    iconStream.WriteByte(1);  // Color planes
                    iconStream.WriteByte(0);  // Color planes
                    iconStream.WriteByte(32); // Bits per pixel
                    iconStream.WriteByte(0);  // Bits per pixel
                    
                    // نوشتن سایز
                    iconStream.WriteByte((byte)(totalSize & 0xFF));
                    iconStream.WriteByte((byte)((totalSize >> 8) & 0xFF));
                    iconStream.WriteByte((byte)((totalSize >> 16) & 0xFF));
                    iconStream.WriteByte((byte)((totalSize >> 24) & 0xFF));
                    
                    // نوشتن آفست
                    iconStream.WriteByte((byte)(imageEntries[i].offset & 0xFF));
                    iconStream.WriteByte((byte)((imageEntries[i].offset >> 8) & 0xFF));
                    iconStream.WriteByte((byte)((imageEntries[i].offset >> 16) & 0xFF));
                    iconStream.WriteByte((byte)((imageEntries[i].offset >> 24) & 0xFF));
                }
                
                // نوشتن داده‌های تصاویر
                foreach (int size in sizes)
                {
                    using (var bitmap = new Bitmap(size, size))
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(sourceImage, 0, 0, size, size);
                        
                        // قفل کردن بیت‌مپ برای دسترسی مستقیم به پیکسل‌ها
                        var bmpData = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb
                        );
                        
                        try
                        {
                            // نوشتن BMP header
                            iconStream.Write(new byte[] { 40, 0, 0, 0 }, 0, 4); // Header size
                            iconStream.Write(BitConverter.GetBytes(bitmap.Width), 0, 4);
                            iconStream.Write(BitConverter.GetBytes(bitmap.Height * 2), 0, 4); // Height * 2 for ICO
                            iconStream.Write(new byte[] { 1, 0 }, 0, 2); // Planes
                            iconStream.Write(new byte[] { 32, 0 }, 0, 2); // Bits per pixel
                            iconStream.Write(new byte[] { 0, 0, 0, 0 }, 0, 4); // Compression
                            iconStream.Write(BitConverter.GetBytes(bitmap.Width * bitmap.Height * 4), 0, 4); // Image size
                            iconStream.Write(BitConverter.GetBytes(0), 0, 4); // X pixels per meter
                            iconStream.Write(BitConverter.GetBytes(0), 0, 4); // Y pixels per meter
                            iconStream.Write(BitConverter.GetBytes(0), 0, 4); // Colors used
                            iconStream.Write(BitConverter.GetBytes(0), 0, 4); // Colors important
                            
                            // نوشتن داده‌های پیکسل در bottom-up order
                            int stride = Math.Abs(bmpData.Stride);
                            int bytesPerPixel = 4;
                            int pixelDataSize = stride * bitmap.Height;
                            byte[] pixelData = new byte[pixelDataSize];
                            
                            Marshal.Copy(bmpData.Scan0, pixelData, 0, pixelDataSize);
                            
                            // نوشتن از پایین به بالا (Windows ICO requirement)
                            for (int y = bitmap.Height - 1; y >= 0; y--)
                            {
                                int rowStart = y * stride;
                                iconStream.Write(pixelData, rowStart, stride);
                            }
                            
                            // نوشتن AND mask با 4-byte alignment
                            var andMaskRowSize = ((bitmap.Width + 31) / 32) * 4;
                            var andMaskSize = andMaskRowSize * bitmap.Height;
                            var andMask = new byte[andMaskSize];
                            iconStream.Write(andMask, 0, andMask.Length);
                        }
                        finally
                        {
                            bitmap.UnlockBits(bmpData);
                        }
                    }
                }
            }
            
            return outputPath;
        }
        
        // حذف فایل با مدیریت قفل‌ها
        public static bool DeleteFileWithRetry(string filePath)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        return true;
                    }
                }
                catch
                {
                    if (i < 2)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
            return false;
        }
        
        // بررسی فرمت فایل تصویری
        public static bool IsImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg" || 
                   extension == ".bmp" || extension == ".gif" || extension == ".ico";
        }
    }
}
