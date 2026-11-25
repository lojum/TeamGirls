using BusinessLogic.Services;
using Microsoft.EntityFrameworkCore;
using SmartLib.IntegrationTests.Infrastructure;
using SmartLib.IntegrationTests.TestData;
using static SmartLib.IntegrationTests.TestData.SecurityTestData;

namespace SmartLib.IntegrationTests.Tests
{
    // Базова перевірка безпеки на справжній PostgreSQL: SQL-ін'єкції і зберігання паролів
    [Trait("Category", "Integration")]
    public class SecurityIntegrationTests : IntegrationTestBase
    {
        private const string CountBooksSql = "SELECT count(*) FROM books";
        private const string CountUsersSql = "SELECT count(*) FROM users";

        public SecurityIntegrationTests(PostgresServer server) : base(server)
        {
        }

        [Theory]
        [MemberData(nameof(SqlInjectionAttempts), MemberType = typeof(SecurityTestData))]
        public async Task BookSearch_SqlInjection_ReturnsNothingAndChangesNothing(string attack)
        {
            // Arrange
            await using var context = Db.CreateContext();
            var booksBefore = await Db.ScalarAsync<long>(CountBooksSql);
            var usersBefore = await Db.ScalarAsync<long>(CountUsersSql);

            // Act
            var results = await new BookService(context).SearchBooksAsync(attack);

            // Assert: рядок шукається як звичайний текст, таблиці не змінилися
            Assert.Empty(results);
            Assert.Equal(booksBefore, await Db.ScalarAsync<long>(CountBooksSql));
            Assert.Equal(usersBefore, await Db.ScalarAsync<long>(CountUsersSql));
        }

        [Theory]
        [MemberData(nameof(SqlInjectionAttempts), MemberType = typeof(SecurityTestData))]
        public async Task Login_SqlInjectionInEmailAndPassword_ReturnsNull(string attack)
        {
            // Arrange
            await using var context = Db.CreateContext();

            // Act
            var user = await new AuthService(context).AuthenticateAsync(attack, attack);

            // Assert
            Assert.Null(user);
        }

        [Fact]
        public async Task PasswordHashes_InDatabase_AreNeverPlainText()
        {
            // Arrange
            await using var context = Db.CreateContext();

            // Act
            var hashes = await context.Users.Select(u => u.PasswordHash).ToListAsync();

            // Assert: кожен пароль збережено лише як хеш PBKDF2 або BCrypt
            Assert.NotEmpty(hashes);
            Assert.All(hashes, hash => Assert.True(
                hash.StartsWith(Pbkdf2Prefix, StringComparison.Ordinal)
                || hash.StartsWith(BcryptPrefix, StringComparison.Ordinal)));
        }
    }
}
