namespace Presentation.Services;

public interface IWindowNavigationService
{
    void ShowMain();
    void ShowLogin();
    void ShowRegistration();
    void ShowAdminDashboard(string adminName);
    void ShowLibrarianDashboard(string librarianName, long librarianId);
    void ShowReaderDashboard(string readerName, long readerId);
}
