using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class ReaderDashboardWindow : Window
    {
        public ReaderDashboardWindow(string readerName)
        {
            InitializeComponent();
            DataContext = new ReaderDashboardViewModel(this, readerName);
        }
    }
}