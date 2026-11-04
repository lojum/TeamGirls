using BusinessLogic.Services;
using Domain.Enums;
using SmartLib.Tests.Helpers;
using SmartLib.Tests.TestData;

namespace SmartLib.Tests.Services
{
    [Trait("Category", "Unit")]
    public class AuthServiceTests
    {
        [Fact]
        public async Task Authenticate_ValidCredentials_ReturnsUser()
        {
            // Arrange
            using var db = TestDb.Create();
            TestDb.AddUser(db, UserTestData.ReaderEmail);

            // Act
            var user = await new AuthService(db).AuthenticateAsync(UserTestData.ReaderEmail, UserTestData.ReaderPassword);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(UserRole.Reader, user.Role);
        }

        // Регресійний тест: раніше вхід порівнював email з урахуванням регістру
        [Theory]
        [MemberData(nameof(UserTestData.ReaderEmailVariants), MemberType = typeof(UserTestData))]
        public async Task Authenticate_EmailInDifferentCaseOrWithSpaces_ReturnsUser(string email)
        {
            // Arrange
            using var db = TestDb.Create();
            TestDb.AddUser(db, UserTestData.ReaderEmail);

            // Act
            var user = await new AuthService(db).AuthenticateAsync(email, UserTestData.ReaderPassword);

            // Assert
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Authenticate_WrongPassword_ReturnsNull()
        {
            // Arrange
            using var db = TestDb.Create();
            TestDb.AddUser(db, UserTestData.ReaderEmail);

            // Act
            var user = await new AuthService(db).AuthenticateAsync(UserTestData.ReaderEmail, UserTestData.WrongPassword);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task Authenticate_UnknownEmail_ReturnsNull()
        {
            // Arrange
            using var db = TestDb.Create();

            // Act
            var user = await new AuthService(db).AuthenticateAsync(UserTestData.UnknownEmail, UserTestData.ReaderPassword);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task Authenticate_InactiveUser_ReturnsNull()
        {
            // Arrange
            using var db = TestDb.Create();
            TestDb.AddUser(db, UserTestData.ReaderEmail, isActive: false);

            // Act
            var user = await new AuthService(db).AuthenticateAsync(UserTestData.ReaderEmail, UserTestData.ReaderPassword);

            // Assert
            Assert.Null(user);
        }

        [Theory]
        [MemberData(nameof(UserTestData.EmptyCredentials), MemberType = typeof(UserTestData))]
        public async Task Authenticate_EmptyInput_ReturnsNull(string email, string password)
        {
            // Arrange
            using var db = TestDb.Create();
            TestDb.AddUser(db, UserTestData.ReaderEmail);

            // Act
            var user = await new AuthService(db).AuthenticateAsync(email, password);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public void NormalizeEmail_TrimsAndLowercases()
        {
            // Act
            var normalized = AuthService.NormalizeEmail("  Olena.B@Gmail.COM ");

            // Assert
            Assert.Equal(UserTestData.ReaderEmail, normalized);
        }
    }
}
