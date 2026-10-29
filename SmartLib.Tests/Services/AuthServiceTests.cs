using BusinessLogic.Services;
using Domain.Enums;
using SmartLib.Tests.Helpers;

namespace SmartLib.Tests.Services
{
    public class AuthServiceTests
    {
        [Fact]
        public async Task Authenticate_ValidCredentials_ReturnsUser()
        {
            using var db = TestDb.Create();
            TestDb.AddUser(db, "olena.b@gmail.com");

            var user = await new AuthService(db).AuthenticateAsync("olena.b@gmail.com", TestDb.ReaderPassword);

            Assert.NotNull(user);
            Assert.Equal(UserRole.Reader, user.Role);
        }

        // Регресійний тест: раніше вхід порівнював email з урахуванням регістру
        [Theory]
        [InlineData("Olena.B@Gmail.com")]
        [InlineData("  olena.b@gmail.com  ")]
        [InlineData("OLENA.B@GMAIL.COM")]
        public async Task Authenticate_EmailInDifferentCaseOrWithSpaces_ReturnsUser(string email)
        {
            using var db = TestDb.Create();
            TestDb.AddUser(db, "olena.b@gmail.com");

            var user = await new AuthService(db).AuthenticateAsync(email, TestDb.ReaderPassword);

            Assert.NotNull(user);
        }

        [Fact]
        public async Task Authenticate_WrongPassword_ReturnsNull()
        {
            using var db = TestDb.Create();
            TestDb.AddUser(db, "olena.b@gmail.com");

            Assert.Null(await new AuthService(db).AuthenticateAsync("olena.b@gmail.com", "wrong-password"));
        }

        [Fact]
        public async Task Authenticate_UnknownEmail_ReturnsNull()
        {
            using var db = TestDb.Create();

            Assert.Null(await new AuthService(db).AuthenticateAsync("nobody@gmail.com", TestDb.ReaderPassword));
        }

        [Fact]
        public async Task Authenticate_InactiveUser_ReturnsNull()
        {
            using var db = TestDb.Create();
            TestDb.AddUser(db, "olena.b@gmail.com", isActive: false);

            Assert.Null(await new AuthService(db).AuthenticateAsync("olena.b@gmail.com", TestDb.ReaderPassword));
        }

        [Theory]
        [InlineData("", TestDb.ReaderPassword)]
        [InlineData("   ", TestDb.ReaderPassword)]
        [InlineData("olena.b@gmail.com", "")]
        public async Task Authenticate_EmptyInput_ReturnsNull(string email, string password)
        {
            using var db = TestDb.Create();
            TestDb.AddUser(db, "olena.b@gmail.com");

            Assert.Null(await new AuthService(db).AuthenticateAsync(email, password));
        }

        [Fact]
        public void NormalizeEmail_TrimsAndLowercases()
        {
            Assert.Equal("olena.b@gmail.com", AuthService.NormalizeEmail("  Olena.B@Gmail.COM "));
        }
    }
}
