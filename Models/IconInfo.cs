using System.IO;

namespace FolderIconManager.WPF.Models
{
    public class IconInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string FullPath => System.IO.Path.GetFullPath(Path);
        public bool IsIconFile => Path.ToLower().EndsWith(".ico");
        public string FileExtension => System.IO.Path.GetExtension(Path).ToLower();
        public long FileSize => File.Exists(Path) ? new FileInfo(Path).Length : 0;

        public IconInfo(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileNameWithoutExtension(path);
        }

        public IconInfo() { }
    }
}
