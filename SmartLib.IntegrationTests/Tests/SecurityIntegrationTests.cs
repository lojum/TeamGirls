using BusinessLogic.Services;
using Microsoft.EntityFrameworkCore;
using SmartLib.IntegrationTests.Infrastructure;
using SmartLib.IntegrationTests.TestData;
using Xunit;

namespace SmartLib.IntegrationTests.Tests
{
    [Trait("Category", "Integration")]
    public class SecurityIntegrationTests : IntegrationTestBase
    {
        public SecurityIntegrationTests(PostgresServer server) : base(server)
        {
        }

        [Fact]
        public async Task BookSearch_SqlInjection_ReturnsNothingAndChangesNothing()
        {
            await using var context = Db.CreateContext();
            var booksBefore = await Db.ScalarAsync<long>("SELECT count(*) FROM books");
            var usersBefore = await Db.ScalarAsync<long>("SELECT count(*) FROM users");
            var service = new BookService(context);

            foreach (var attack in SecurityTestData.SqlInjectionPayloads)
            {
                var results = await service.SearchBooksAsync(attack);

                Assert.Empty(results);
                Assert.Equal(booksBefore, await Db.ScalarAsync<long>("SELECT count(*) FROM books"));
                Assert.Equal(usersBefore, await Db.ScalarAsync<long>("SELECT count(*) FROM users"));
            }
        }

        [Fact]
        public async Task Login_SqlInjectionInEmail_ReturnsNull()
        {
            await using var context = Db.CreateContext();
            var service = new AuthService(context);

            foreach (var attack in SecurityTestData.SqlInjectionPayloads)
            {
                var user = await service.AuthenticateAsync(attack, attack);
                Assert.Null(user);
            }
        }

        [Fact]
        public async Task PasswordHashes_InDatabase_AreNeverPlainText()
        {
            await using var context = Db.CreateContext();

            var hashes = await context.Users
                .Select(u => u.PasswordHash)
                .ToListAsync();

            Assert.NotEmpty(hashes);
            foreach (var hash in hashes)
            {
                Assert.False(string.IsNullOrWhiteSpace(hash));
                Assert.True(
                    hash.StartsWith("pbkdf2-sha256$", StringComparison.Ordinal) ||
                    hash.StartsWith("$2", StringComparison.Ordinal),
                    $"Хеш пароля не схожий на безпечний хеш: {hash}");
            }
        }
    }
}