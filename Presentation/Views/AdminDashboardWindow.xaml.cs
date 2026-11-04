using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class AdminDashboardWindow : Window
    {
        public AdminDashboardWindow(string currentAdminName, AdminDashboardViewModel viewModel)
        {
            InitializeComponent();
            viewModel.SetAdminName(currentAdminName);
            DataContext = viewModel;
            Loaded += async (_, _) => await viewModel.LoadAsync();
        }
    }
}