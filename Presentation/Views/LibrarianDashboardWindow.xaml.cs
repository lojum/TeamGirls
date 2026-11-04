using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views;

public partial class LibrarianDashboardWindow : Window
{
    public LibrarianDashboardWindow(string librarianName, LibrarianDashboardViewModel viewModel)
    {
        InitializeComponent();
        viewModel.SetLibrarianName(librarianName);
        DataContext = viewModel;
    }
}
