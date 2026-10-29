using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        public AuthService(SmartLibDbContext context)
        {
            _context = context;
        }

        // Унікальний індекс у базі побудований на lower(email),
        // тому email завжди порівнюємо без урахування регістру
        public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
                return null;

            var normalizedEmail = NormalizeEmail(email);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

            if (user == null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash))
                return null;

            return user;
        }
    }
}
