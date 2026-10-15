using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views 
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            DataContext = new MainViewModel(this);
        }
    }
}