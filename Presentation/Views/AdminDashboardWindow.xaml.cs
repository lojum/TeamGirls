using System.Windows;
using Npgsql;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class AdminDashboardWindow : Window
    {
        private readonly AdminDashboardViewModel _viewModel;

        public AdminDashboardWindow(string currentAdminName = "Library Admin")
        {
            InitializeComponent();
            _viewModel = new AdminDashboardViewModel(this, currentAdminName);
            DataContext = _viewModel;
            Loaded += LoadDashboard;
        }

        private async void LoadDashboard(object sender, RoutedEventArgs e)
        {
            Loaded -= LoadDashboard;
            await RefreshDashboardAsync();
        }

        private async void RefreshDashboard_Click(object sender, RoutedEventArgs e) =>
            await RefreshDashboardAsync();

        private async Task RefreshDashboardAsync()
        {
            RefreshButton.IsEnabled = false;
            try
            {
                await _viewModel.LoadAsync();
            }
            catch (NpgsqlException)
            {
                MessageBox.Show(
                    this,
                    "Could not load dashboard data. Check the database connection and try again.",
                    "Database connection error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show(
                    this,
                    "Could not initialize dashboard data. Check the application database configuration.",
                    "Dashboard error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }
    }
}
