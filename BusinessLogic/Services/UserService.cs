using DataAccess;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace BusinessLogic.Services;

public interface IUserService
{
    Task RegisterReaderAsync(string fullName, string email, string password);
}

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email, Exception innerException)
        : base($"An account with email '{email}' already exists.", innerException)
    {
    }
}

// База відхилила ім'я або email за правилами DOMAIN (наприклад, email без домену верхнього рівня)
public sealed class InvalidUserDataException : Exception
{
    public InvalidUserDataException(Exception innerException)
        : base("The name or email address has an invalid format.", innerException)
    {
    }
}

public sealed class UserService : IUserService
{
    private readonly SmartLibDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(SmartLibDbContext context, ILogger<UserService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<UserService>.Instance;
    }

    public async Task RegisterReaderAsync(string fullName, string email, string password)
    {
        var user = new User
        {
            FullName = fullName.Trim(),
            Email = AuthService.NormalizeEmail(email),
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
            _context.Entry(user).State = EntityState.Detached;
            _logger.LogWarning("Registration rejected: email is already in use");
            throw new DuplicateEmailException(user.Email, exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.CheckViolation
            } check)
        {
            _context.Entry(user).State = EntityState.Detached;
            _logger.LogWarning("Registration rejected: data violates database rule {Constraint}", check.ConstraintName);
            throw new InvalidUserDataException(exception);
        }
        catch (Exception exception)
        {
            _context.Entry(user).State = EntityState.Detached;
            _logger.LogError(exception, "Failed to register a reader");
            throw;
        }

        _logger.LogInformation("Reader {UserId} registered", user.UserId);
    }
}
