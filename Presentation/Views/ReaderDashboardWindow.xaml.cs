using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class ReaderDashboardWindow : Window
    {
        public ReaderDashboardWindow(string readerName, ReaderDashboardViewModel viewModel)
        {
            InitializeComponent();
            viewModel.SetReaderName(readerName);
            DataContext = viewModel;
        }
    }
}