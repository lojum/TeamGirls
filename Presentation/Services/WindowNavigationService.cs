using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Views;

namespace Presentation.Services;

public sealed class WindowNavigationService : IWindowNavigationService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WindowNavigationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ShowMain() => NavigateTo<MainWindow>();
    public void ShowLogin() => NavigateTo<LoginWindow>();
    public void ShowRegistration() => NavigateTo<RegistrationWindow>();

    public void ShowAdminDashboard(string adminName) =>
        NavigateTo<AdminDashboardWindow>(adminName);

    public void ShowLibrarianDashboard(string librarianName) =>
        NavigateTo<LibrarianDashboardWindow>(librarianName);

    public void ShowReaderDashboard(string readerName) =>
        NavigateTo<ReaderDashboardWindow>(readerName);

    private void NavigateTo<TWindow>(object? parameter = null)
        where TWindow : Window
    {
        var scope = _scopeFactory.CreateScope();
        var previousWindow = Application.Current.MainWindow;

        try
        {
            var window = parameter is null
                ? scope.ServiceProvider.GetRequiredService<TWindow>()
                : ActivatorUtilities.CreateInstance<TWindow>(scope.ServiceProvider, parameter);

            Application.Current.MainWindow = window;
            window.Closed += (_, _) => scope.Dispose();
            window.Show();
            previousWindow?.Close();
        }
        catch
        {
            scope.Dispose();
            throw;
        }
    }
}
