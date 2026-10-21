using System.Windows;
using System.Windows.Input;
using Presentation.Commands;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public class MainViewModel
    {
        private readonly Window _currentWindow;

        public ICommand OpenLoginCommand { get; }
        public ICommand OpenRegisterCommand { get; }

        public MainViewModel(Window currentWindow)
        {
            _currentWindow = currentWindow;
            OpenLoginCommand = new RelayCommand(_ => OpenWindow(new LoginWindow()));
            OpenRegisterCommand = new RelayCommand(_ => OpenWindow(new RegistrationWindow()));
        }

        private void OpenWindow(Window window)
        {
            window.Show();
            Application.Current.MainWindow = window;
            _currentWindow.Close();
        }
    }
}
