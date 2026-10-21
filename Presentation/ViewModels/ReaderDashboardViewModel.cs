using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Presentation.Commands;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public class ReaderDashboardViewModel : INotifyPropertyChanged
    {
        private readonly Window _currentWindow;
        private string _readerName;

        public string ReaderName { get => _readerName; set { _readerName = value; OnPropertyChanged(); } }

        public ICommand LogOutCommand { get; }

        public ReaderDashboardViewModel(Window currentWindow, string readerName)
        {
            _currentWindow = currentWindow;
            _readerName = readerName;
            LogOutCommand = new RelayCommand(ExecuteLogOut);
        }

        private void ExecuteLogOut(object? obj)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Application.Current.MainWindow = mainWindow;
            _currentWindow.Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}