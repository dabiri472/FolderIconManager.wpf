using System.Windows;
using System.Windows.Controls;
using FolderIconManager.WPF.ViewModels;
using FolderIconManager.WPF.Models;

namespace FolderIconManager.WPF
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = DataContext as MainViewModel;
            
            // اتصال رویداد انتخاب درایوها
            if (DrivesListView != null)
            {
                DrivesListView.SelectionChanged += DrivesListView_SelectionChanged;
            }
        }

        private void DrivesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null || DrivesListView == null) return;

            // پاک کردن انتخاب‌های قبلی
            _viewModel.SelectedDrives.Clear();

            // اضافه کردن درایوهای انتخاب شده
            foreach (DriveInfoModel drive in DrivesListView.SelectedItems)
            {
                _viewModel.SelectedDrives.Add(drive);
            }
        }
    }
}
