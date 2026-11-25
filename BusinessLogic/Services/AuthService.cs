using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Domain.Entities;
using DataAccess;

namespace BusinessLogic.Services
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string email, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly SmartLibDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(SmartLibDbContext context, ILogger<AuthService>? logger = null)
        {
            _context = context;
            _logger = logger ?? NullLogger<AuthService>.Instance;
        }

        // Унікальний індекс у базі побудований на lower(email),
        // тому email завжди порівнюємо без урахування регістру
        public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
                return null;

            var normalizedEmail = NormalizeEmail(email);
            User? user;
            try
            {
                user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Database error during sign-in");
                throw;
            }

            // Персональні дані (email, пароль, хеш) у журнал не пишемо: лише id користувача, якщо він існує, і причину
            if (user == null)
            {
                _logger.LogWarning("Failed sign-in: account not found");
                return null;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Failed sign-in for user {UserId}: account is inactive", user.UserId);
                return null;
            }

            if (!PasswordHasher.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Failed sign-in for user {UserId}: wrong password", user.UserId);
                return null;
            }

            _logger.LogInformation("User {UserId} signed in as {Role}", user.UserId, user.Role);
            return user;
        }
    }
}
