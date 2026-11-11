using BusinessLogic.Services;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
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
    }
}
