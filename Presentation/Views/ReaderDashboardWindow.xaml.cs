using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using BusinessLogic.Services;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class ReaderDashboardWindow : Window
    {
        public ReaderDashboardWindow(string readerName)
        {
            InitializeComponent();
            
            var scope = App.ServiceProvider.CreateScope();
            var bookService = scope.ServiceProvider.GetRequiredService<IBookService>();
            
            DataContext = new ReaderDashboardViewModel(this, readerName, bookService);
        }
    }
}