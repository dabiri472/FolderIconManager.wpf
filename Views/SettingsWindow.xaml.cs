using System.Windows;
using FolderIconManager.WPF.ViewModels;

namespace FolderIconManager.WPF.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}
