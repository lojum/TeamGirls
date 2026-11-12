using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Presentation.Commands;
using BusinessLogic.Services;
using Presentation.Services;
using Presentation.Validators;

namespace Presentation.ViewModels
{
    public class RegistrationViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly IWindowNavigationService _navigationService;

        private string _fullName = "";
        private string _email = "";
        private string _password = "";
        private string _confirmPassword = "";

        private Visibility _nameErrorVisibility = Visibility.Collapsed;
        private Visibility _emailErrorVisibility = Visibility.Collapsed;
        private Visibility _passwordErrorVisibility = Visibility.Collapsed;
        private Visibility _confirmPasswordErrorVisibility = Visibility.Collapsed;
        private bool _isSubmitEnabled;

        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); ValidateInput(); } }
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); ValidateInput(); } }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); ValidateInput(); } }
        public string ConfirmPassword { get => _confirmPassword; set { _confirmPassword = value; OnPropertyChanged(); ValidateInput(); } }

        public Visibility NameErrorVisibility { get => _nameErrorVisibility; set { _nameErrorVisibility = value; OnPropertyChanged(); } }
        public Visibility EmailErrorVisibility { get => _emailErrorVisibility; set { _emailErrorVisibility = value; OnPropertyChanged(); } }
        public Visibility PasswordErrorVisibility { get => _passwordErrorVisibility; set { _passwordErrorVisibility = value; OnPropertyChanged(); } }
        public Visibility ConfirmPasswordErrorVisibility { get => _confirmPasswordErrorVisibility; set { _confirmPasswordErrorVisibility = value; OnPropertyChanged(); } }
        public bool IsSubmitEnabled { get => _isSubmitEnabled; set { _isSubmitEnabled = value; OnPropertyChanged(); } }

        public ICommand RegisterCommand { get; }
        public ICommand BackCommand { get; }

        public RegistrationViewModel(IUserService userService, IWindowNavigationService navigationService)
        {
            _userService = userService;
            _navigationService = navigationService;
            BackCommand = new RelayCommand(ExecuteBack);
            RegisterCommand = new RelayCommand(ExecuteRegister);
        }

        private void ValidateInput()
        {

            IsSubmitEnabled = RegistrationValidator.IsValidInput(FullName, Email, Password, ConfirmPassword);
        }

        private async void ExecuteRegister(object? obj)
        {
            bool isNameValid = RegistrationValidator.IsValidName(FullName);
            NameErrorVisibility = isNameValid ? Visibility.Collapsed : Visibility.Visible;

            bool isEmailValid = RegistrationValidator.IsValidEmail(Email);
            EmailErrorVisibility = isEmailValid ? Visibility.Collapsed : Visibility.Visible;

            bool isPasswordValid = RegistrationValidator.IsValidPassword(Password);
            PasswordErrorVisibility = isPasswordValid ? Visibility.Collapsed : Visibility.Visible;

            bool isConfirmPasswordValid = RegistrationValidator.PasswordsMatch(Password, ConfirmPassword);
            ConfirmPasswordErrorVisibility = isConfirmPasswordValid ? Visibility.Collapsed : Visibility.Visible;

            if (!isNameValid || !isEmailValid || !isPasswordValid || !isConfirmPasswordValid)
                return;

            IsSubmitEnabled = false;

            try
            {
                await _userService.RegisterReaderAsync(FullName, Email, Password);

                MessageBox.Show("Reader account created. You can now sign in.", "Registration complete", MessageBoxButton.OK, MessageBoxImage.Information);

                _navigationService.ShowMain();
            }
            catch (DuplicateEmailException)
            {
                IsSubmitEnabled = true;
                MessageBox.Show("An account with this email address already exists.", "Registration error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception)
            {
                IsSubmitEnabled = true;
                MessageBox.Show("Could not connect to or write to the database. Try again later.", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
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