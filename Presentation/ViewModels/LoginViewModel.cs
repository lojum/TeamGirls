using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Commands;
using BusinessLogic.Services;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly Window _currentWindow;
        
        private string _email = "";
        private string _password = "";
        
        private Visibility _emailErrorVisibility = Visibility.Collapsed;
        private Visibility _passwordErrorVisibility = Visibility.Collapsed;
        private Visibility _loginErrorVisibility = Visibility.Collapsed;
        private string _loginErrorMessage = "";
        private bool _isSubmitEnabled;

        public string Email { get => _email; set { _email = value; OnPropertyChanged(); ValidateInput(); } }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); ValidateInput(); } }

        public Visibility EmailErrorVisibility { get => _emailErrorVisibility; set { _emailErrorVisibility = value; OnPropertyChanged(); } }
        public Visibility PasswordErrorVisibility { get => _passwordErrorVisibility; set { _passwordErrorVisibility = value; OnPropertyChanged(); } }
        public Visibility LoginErrorVisibility { get => _loginErrorVisibility; set { _loginErrorVisibility = value; OnPropertyChanged(); } }
        public string LoginErrorMessage { get => _loginErrorMessage; set { _loginErrorMessage = value; OnPropertyChanged(); } }
        public bool IsSubmitEnabled { get => _isSubmitEnabled; set { _isSubmitEnabled = value; OnPropertyChanged(); } }

        public ICommand LoginCommand { get; }
        public ICommand BackCommand { get; }

        public LoginViewModel(Window currentWindow)
        {
            _currentWindow = currentWindow;
            BackCommand = new RelayCommand(ExecuteBack);
            LoginCommand = new RelayCommand(ExecuteLogin);
        }

        private void ValidateInput()
        {
            IsSubmitEnabled = !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(Password);
            LoginErrorVisibility = Visibility.Collapsed;
        }

        private async void ExecuteLogin(object? obj)
        {
            bool isValid = true;

            if (!MailAddress.TryCreate(Email, out _))
            {
                EmailErrorVisibility = Visibility.Visible;
                isValid = false;
            }
            else EmailErrorVisibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(Password))
            {
                PasswordErrorVisibility = Visibility.Visible;
                isValid = false;
            }
            else PasswordErrorVisibility = Visibility.Collapsed;

            if (!isValid) return;

            IsSubmitEnabled = false;
            LoginErrorVisibility = Visibility.Collapsed;

            try
            {
                using var scope = App.ServiceProvider.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

                var user = await authService.AuthenticateAsync(Email, Password);

                if (user == null)
                {
                    LoginErrorMessage = "Incorrect email or password, or the account is inactive.";
                    LoginErrorVisibility = Visibility.Visible;
                    IsSubmitEnabled = true;
                    return;
                }

                if (user.Role == UserRole.Admin)
                {
                    var dashboard = new AdminDashboardWindow(user.FullName);
                    dashboard.Show();
                    Application.Current.MainWindow = dashboard;
                }
                else if (user.Role == UserRole.Reader)
                {
                    var readerDashboard = new ReaderDashboardWindow(user.FullName);
                    readerDashboard.Show();
                    Application.Current.MainWindow = readerDashboard;
                }
                else if (user.Role == UserRole.Librarian)
                {
                    var librarianDashboard = new LibrarianDashboardWindow(user.FullName, user.UserId);
                    librarianDashboard.Show();
                    Application.Current.MainWindow = librarianDashboard;
                }
                else
                {
                    LoginErrorMessage = "This role interface is not implemented yet.";
                    LoginErrorVisibility = Visibility.Visible;
                    IsSubmitEnabled = true;
                    return;
                }

                _currentWindow.Close();
            }
            catch (Exception)
            {
                LoginErrorMessage = "Could not connect to the database. Try again later.";
                LoginErrorVisibility = Visibility.Visible;
                IsSubmitEnabled = true;
            }
        }

        private void ExecuteBack(object? obj)
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