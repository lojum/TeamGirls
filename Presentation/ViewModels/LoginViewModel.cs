using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Presentation.Commands;
using BusinessLogic.Services;
using Presentation.Services;

namespace Presentation.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly IWindowNavigationService _navigationService;
        
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

        public LoginViewModel(IUserService userService, IWindowNavigationService navigationService)
        {
            _userService = userService;
            _navigationService = navigationService;
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
                var user = await _userService.AuthenticateAsync(Email, Password);
                if (user is null)
                {
                    LoginErrorMessage = "Incorrect email or password, or the account is inactive.";
                    LoginErrorVisibility = Visibility.Visible;
                    IsSubmitEnabled = true;
                    return;
                }

                if (user.IsAdmin)
                {
                    _navigationService.ShowAdminDashboard(user.FullName);
                }
                else if (user.IsReader)
                {
                    _navigationService.ShowReaderDashboard(user.FullName);
                }
                else
                {
                    _navigationService.ShowLibrarianDashboard(user.FullName);
                }

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
            _navigationService.ShowMain();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}