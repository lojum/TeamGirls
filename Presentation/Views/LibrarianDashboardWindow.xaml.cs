using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views;

public partial class LibrarianDashboardWindow : Window
{
    public LibrarianDashboardWindow(string librarianName, long librarianId, LibrarianDashboardViewModel viewModel)
    {
        InitializeComponent();
        viewModel.SetLibrarian(librarianName, librarianId);
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadAsync();
    }
}
