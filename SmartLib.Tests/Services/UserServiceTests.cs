using BusinessLogic.Services;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartLib.Tests.Helpers;
using SmartLib.Tests.TestData;
using static SmartLib.Tests.TestData.ServiceTestData;

namespace SmartLib.Tests.Services
{
    // Повторний email перевіряє унікальний індекс PostgreSQL, тому цей випадок — в інтеграційних тестах
    [Trait("Category", "Unit")]
    public class UserServiceTests
    {
        [Fact]
        public async Task RegisterReader_SavesActiveReaderWithNormalizedEmailAndHashedPassword()
        {
            // Arrange
            using var db = TestDb.Create();

            // Act
            await new UserService(db).RegisterReaderAsync(NewReaderName, NewReaderEmail, NewReaderPassword);

            // Assert
            var user = await db.Users.SingleAsync();
            Assert.Equal(NewReaderName.Trim(), user.FullName);
            Assert.Equal(AuthService.NormalizeEmail(NewReaderEmail), user.Email);
            Assert.Equal(UserRole.Reader, user.Role);
            Assert.True(user.IsActive);
            Assert.NotEqual(NewReaderPassword, user.PasswordHash);
            Assert.True(PasswordHasher.Verify(NewReaderPassword, user.PasswordHash));
        }

        [Fact]
        public async Task RegisterReader_Success_LogsNewReaderWithoutPersonalData()
        {
            // Arrange
            using var db = TestDb.Create();
            var logger = new ListLogger<UserService>();

            // Act
            await new UserService(db, logger).RegisterReaderAsync(NewReaderName, NewReaderEmail, NewReaderPassword);

            // Assert
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.DoesNotContain(NewReaderEmail.Trim(), entry.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(NewReaderPassword, entry.Message);
        }

        [Fact]
        public async Task RegisterReader_DuplicateEmail_LogsWarningWithoutEmail()
        {
            // Arrange: унікальний індекс бази відхиляє повторний email
            var failure = new ThrowOnSaveInterceptor(new DbUpdateException("duplicate", new Npgsql.PostgresException(
                "duplicate key", "ERROR", "ERROR", Npgsql.PostgresErrorCodes.UniqueViolation,
                constraintName: "ux_users_email")));
            using var db = TestDb.Create(failure);
            failure.Enabled = true;
            var logger = new ListLogger<UserService>();

            // Act
            await Assert.ThrowsAsync<DuplicateEmailException>(
                () => new UserService(db, logger).RegisterReaderAsync(NewReaderName, NewReaderEmail, NewReaderPassword));

            // Assert
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.DoesNotContain(NewReaderEmail.Trim(), entry.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RegisteredReader_CanLogInWithEmailInAnyCase()
        {
            // Arrange
            using var db = TestDb.Create();
            await new UserService(db).RegisterReaderAsync(NewReaderName, NewReaderEmail, NewReaderPassword);

            // Act
            var user = await new AuthService(db).AuthenticateAsync(NewReaderEmail.ToUpperInvariant(), NewReaderPassword);

            // Assert
            Assert.NotNull(user);
        }

        [Fact]
        public void DuplicateEmailException_KeepsEmailAndOriginalError()
        {
            // Arrange
            var inner = new InvalidOperationException("unique violation");

            // Act
            var ex = new DuplicateEmailException(UserTestData.ReaderEmail, inner);

            // Assert
            Assert.Contains(UserTestData.ReaderEmail, ex.Message);
            Assert.Same(inner, ex.InnerException);
        }

        // База відхилила email правилом DOMAIN: зрозумілий виняток замість «Could not connect»
        [Fact]
        public async Task RegisterReader_CheckViolation_ThrowsInvalidUserDataAndLogsWarning()
        {
            // Arrange
            var failure = new ThrowOnSaveInterceptor(new DbUpdateException("check", new Npgsql.PostgresException(
                "value violates check constraint", "ERROR", "ERROR", Npgsql.PostgresErrorCodes.CheckViolation,
                constraintName: "email_address_check")));
            using var db = TestDb.Create(failure);
            failure.Enabled = true;
            var logger = new ListLogger<UserService>();

            // Act
            await Assert.ThrowsAsync<InvalidUserDataException>(
                () => new UserService(db, logger).RegisterReaderAsync(NewReaderName, NewReaderEmail, NewReaderPassword));

            // Assert: невдалий запис прибрано з контексту, у журналі — назва правила, без пароля
            Assert.Empty(db.ChangeTracker.Entries());
            var entry = Assert.Single(logger.Entries);
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Contains("email_address_check", entry.Message);
            Assert.DoesNotContain(NewReaderPassword, entry.Message);
        }
    }
}
