using System.Windows;
using System.Windows.Input;
using Presentation.Commands;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public class MainViewModel
    {
        private Window _currentWindow;

        public ICommand OpenLoginCommand { get; }
        public ICommand OpenRegisterCommand { get; }

        public MainViewModel(Window currentWindow)
        {
            _currentWindow = currentWindow;

            OpenLoginCommand = new RelayCommand(ExecuteOpenLogin);
            OpenRegisterCommand = new RelayCommand(ExecuteOpenRegister);
        }

        private void ExecuteOpenLogin(object? obj)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            _currentWindow.Close();
        }

        private void ExecuteOpenRegister(object? obj)
        {
            var regWindow = new RegistrationWindow();
            regWindow.Show();
            _currentWindow.Close();
        }
    }
}