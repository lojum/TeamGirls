using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class AdminDashboardWindow : Window
    {
        public AdminDashboardWindow(string currentAdminName)
        {
            InitializeComponent();
            
            DataContext = new AdminDashboardViewModel(this, currentAdminName);
        }
    }
}