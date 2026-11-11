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

    public void ShowLibrarianDashboard(string librarianName, long librarianId) =>
        NavigateTo<LibrarianDashboardWindow>(librarianName, librarianId);

    public void ShowReaderDashboard(string readerName, long readerId) =>
        NavigateTo<ReaderDashboardWindow>(readerName, readerId);

    private void NavigateTo<TWindow>(params object[] parameters)
        where TWindow : Window
    {
        var scope = _scopeFactory.CreateScope();
        var previousWindow = Application.Current.MainWindow;

        try
        {
            var window = parameters.Length == 0
                ? scope.ServiceProvider.GetRequiredService<TWindow>()
                : ActivatorUtilities.CreateInstance<TWindow>(scope.ServiceProvider, parameters);

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
