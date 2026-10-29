using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DataAccess;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Presentation.Commands;
using BusinessLogic.Services;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public class RegistrationViewModel : INotifyPropertyChanged
    {
        private readonly Window _currentWindow;

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

        public RegistrationViewModel(Window currentWindow)
        {
            _currentWindow = currentWindow;
            BackCommand = new RelayCommand(ExecuteBack);
            RegisterCommand = new RelayCommand(ExecuteRegister);
        }

        private void ValidateInput()
        {
            IsSubmitEnabled = !string.IsNullOrWhiteSpace(FullName) && 
                              !string.IsNullOrWhiteSpace(Email) && 
                              !string.IsNullOrEmpty(Password) && 
                              !string.IsNullOrEmpty(ConfirmPassword);
        }

        private async void ExecuteRegister(object? obj)
        {
            bool isValid = true;

            if (FullName.Trim().Length == 0 || FullName.Length > 150)
            {
                NameErrorVisibility = Visibility.Visible;
                isValid = false;
            }
            else NameErrorVisibility = Visibility.Collapsed;

            if (!MailAddress.TryCreate(Email, out _))
            {
                EmailErrorVisibility = Visibility.Visible;
                isValid = false;
            }
            else EmailErrorVisibility = Visibility.Collapsed;

            if (Password.Length < 8)
            {
                PasswordErrorVisibility = Visibility.Visible;
                isValid = false;
            }
            else PasswordErrorVisibility = Visibility.Collapsed;

            if (Password != ConfirmPassword)
            {
                ConfirmPasswordErrorVisibility = Visibility.Visible;
                isValid = false;
            }
            else ConfirmPasswordErrorVisibility = Visibility.Collapsed;

            if (!isValid) return;

            IsSubmitEnabled = false;

            try
            {
                using var scope = App.ServiceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SmartLibDbContext>();

                var user = new User
                {
                    FullName = FullName.Trim(),
                    Email = AuthService.NormalizeEmail(Email),
                    PasswordHash = PasswordHasher.Hash(Password), 
                    Role = UserRole.Reader
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();

                MessageBox.Show("Reader account created. Reader sign-in will be available later.", "Registration complete", MessageBoxButton.OK, MessageBoxImage.Information);

                var mainWindow = new MainWindow();
                mainWindow.Show();
                Application.Current.MainWindow = mainWindow;
                _currentWindow.Close();
            }
            catch (DbUpdateException ex)
            {
                IsSubmitEnabled = true;
                
                // Перевіряємо, чи це саме помилка унікальності PostgreSQL (код 23505)
                if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    MessageBox.Show("An account with this email address already exists.", "Registration error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("The database rejected the registration. Please check the entered information.", "Registration error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception)
            {
                IsSubmitEnabled = true;
                MessageBox.Show("Could not connect to or write to the database. Try again later.", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
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