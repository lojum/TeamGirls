using DataAccess;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;
using Presentation.Commands;
using Presentation.Services;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public class LoginViewModel
    {
        private Window _currentWindow;

        public ICommand BackCommand { get; }

        public LoginViewModel(Window currentWindow)
        {
            _currentWindow = currentWindow;
            BackCommand = new RelayCommand(ExecuteBack);
        }

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            using var scope = App.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SmartLibDbContext>();
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Email.ToLower() == normalizedEmail);

            if (user is null
                || !user.IsActive
                || !PasswordHasher.Verify(password, user.PasswordHash))
            {
                return null;
            }

            return user;
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