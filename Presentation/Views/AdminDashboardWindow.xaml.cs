using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class AdminDashboardWindow : Window
    {
        public AdminDashboardWindow(string currentAdminName)
        {
            InitializeComponent();
            
            var viewModel = new AdminDashboardViewModel(this, currentAdminName);
            DataContext = viewModel;
            Loaded += async (_, _) =>
            {
                try
                {
                    await viewModel.LoadAsync();
                }
                catch (Exception)
                {
                    MessageBox.Show("Could not load dashboard data. Try again later.", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }
    }
}