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

            Loaded += async (sender, args) => await viewModel.LoadAsync();
        }
    }
}