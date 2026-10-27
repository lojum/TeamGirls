using System.Windows;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();
            DataContext = new RegistrationViewModel(this);
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegistrationViewModel viewModel)
            {
                viewModel.Password = PasswordInput.Password;
            }
        }

        private void ConfirmPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegistrationViewModel viewModel)
            {
                viewModel.ConfirmPassword = ConfirmPasswordInput.Password;
            }
        }
    }
}