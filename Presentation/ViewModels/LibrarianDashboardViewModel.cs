using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BusinessLogic.Services;
using Presentation.Commands;
using Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.ViewModels
{
    public class LibrarianDashboardViewModel : INotifyPropertyChanged
    {
        private readonly Window _currentWindow;
        private readonly long _librarianId;

        private string _readerEmail = "";
        private string _inventoryNumber = "";
        private string _statusMessage = "";
        private bool _isError;

        public string LibrarianName { get; }
        
        public string ReaderEmail { get => _readerEmail; set { _readerEmail = value; OnPropertyChanged(); } }
        public string InventoryNumber { get => _inventoryNumber; set { _inventoryNumber = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsError { get => _isError; set { _isError = value; OnPropertyChanged(); } }

        public ICommand IssueBookCommand { get; }
        public ICommand LogoutCommand { get; }

        public LibrarianDashboardViewModel(Window currentWindow, string librarianName, long librarianId)
        {
            _currentWindow = currentWindow;
            LibrarianName = librarianName;
            _librarianId = librarianId;

            IssueBookCommand = new RelayCommand(ExecuteIssueBook);
            LogoutCommand = new RelayCommand(ExecuteLogout);
        }

        private async void ExecuteIssueBook(object? obj)
        {
            if (string.IsNullOrWhiteSpace(ReaderEmail) || string.IsNullOrWhiteSpace(InventoryNumber))
            {
                IsError = true;
                StatusMessage = "Please fill in all the fields.";
                return;
            }

            StatusMessage = "Processing...";
            IsError = false;

            try
            {
                using var scope = App.ServiceProvider.CreateScope();
                var loanService = scope.ServiceProvider.GetRequiredService<ILoanService>();
                
                loanService.BookLoaned += OnBookLoaned;

                await loanService.IssueBookAsync(InventoryNumber, ReaderEmail, _librarianId);
                
                loanService.BookLoaned -= OnBookLoaned;

                ReaderEmail = "";
                InventoryNumber = "";
            }
            catch (Exception ex)
            {
                IsError = true;
                StatusMessage = ex.Message; 
            }
        }

        private void OnBookLoaned(object? sender, LoanEventArgs e)
        {
            IsError = false;
            StatusMessage = $"Success: Book '{e.BookTitle}' issued to reader {e.ReaderName}!";
        }

        private void ExecuteLogout(object? obj)
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