using System.Windows;
using System.Windows.Input;
using DataAccess;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Presentation.Commands;
using Presentation.Services;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public class RegistrationViewModel
    {
        private readonly Window _currentWindow;

        public ICommand BackCommand { get; }

        public RegistrationViewModel(Window currentWindow)
        {
            _currentWindow = currentWindow;
            BackCommand = new RelayCommand(ExecuteBack);
        }

        public async Task<UserRole> RegisterAsync(string fullName, string email, string password)
        {
            using var scope = App.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SmartLibDbContext>();

            var user = new User
            {
                FullName = fullName.Trim(),
                Email = email.Trim(),
                PasswordHash = PasswordHasher.Hash(password),
                Role = UserRole.Reader
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return user.Role;
        }

        private void ExecuteBack(object? obj)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Application.Current.MainWindow = mainWindow;
            _currentWindow.Close();
        }
    }
}