using System.Windows;
using System.Windows.Input;
using Presentation.Commands;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public class RegistrationViewModel
    {
        private Window _currentWindow;

        public ICommand BackCommand { get; }

        public RegistrationViewModel(Window currentWindow)
        {
            _currentWindow = currentWindow;
            BackCommand = new RelayCommand(ExecuteBack);
        }

        private void ExecuteBack(object? obj)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            _currentWindow.Close();
        }
    }
}