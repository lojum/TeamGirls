using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class LibrarianDashboardWindow : Window
    {
        public LibrarianDashboardWindow(string librarianName, long librarianId)
        {
            InitializeComponent();
            DataContext = new LibrarianDashboardViewModel(this, librarianName, librarianId);
        }
    }
}