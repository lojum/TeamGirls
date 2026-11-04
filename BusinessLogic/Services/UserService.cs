using DataAccess;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BusinessLogic.Services;

public interface IUserService
{
    Task<AuthenticatedUser?> AuthenticateAsync(string email, string password);
    Task RegisterReaderAsync(string fullName, string email, string password);
}

public sealed record AuthenticatedUser(string FullName, bool IsAdmin, bool IsReader);

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email, Exception innerException)
        : base($"An account with email '{email}' already exists.", innerException)
    {
    }
}

public sealed class UserService : IUserService
{
    private readonly SmartLibDbContext _context;

    public UserService(SmartLibDbContext context)
    {
        _context = context;
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(string email, string password)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email == email.Trim());

        if (user is null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash))
        {
            return null;
        }

        return new AuthenticatedUser(
            user.FullName,
            user.Role == UserRole.Admin,
            user.Role == UserRole.Reader);
    }

    public async Task RegisterReaderAsync(string fullName, string email, string password)
    {
        var user = new User
        {
            FullName = fullName.Trim(),
            Email = email.Trim(),
            PasswordHash = PasswordHasher.Hash(password),
            Role = UserRole.Reader
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new DuplicateEmailException(user.Email, exception);
        }
    }
}
