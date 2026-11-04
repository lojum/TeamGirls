namespace Presentation.Services;

public interface IWindowNavigationService
{
    void ShowMain();
    void ShowLogin();
    void ShowRegistration();
    void ShowAdminDashboard(string adminName);
    void ShowLibrarianDashboard(string librarianName);
    void ShowReaderDashboard(string readerName);
}
