using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            DataContext = new LoginViewModel(this);
        }
    }
}
