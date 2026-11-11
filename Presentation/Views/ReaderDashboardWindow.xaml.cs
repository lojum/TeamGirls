using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class ReaderDashboardWindow : Window
    {
        public ReaderDashboardWindow(string readerName, long readerId, ReaderDashboardViewModel viewModel)
        {
            InitializeComponent();
            viewModel.SetReader(readerName, readerId);
            DataContext = viewModel;
            Loaded += async (_, _) => await viewModel.LoadAsync();
        }
    }
}
