using Npgsql;
using SmartLib.IntegrationTests.Infrastructure;
using SmartLib.IntegrationTests.TestData;
using static SmartLib.IntegrationTests.TestData.ConstraintTestData;

namespace SmartLib.IntegrationTests.Tests
{
    // Обмеження, які працюють лише в справжній PostgreSQL: DOMAIN, CHECK і унікальні індекси
    [Trait("Category", "Integration")]
    public class SchemaConstraintTests : IntegrationTestBase
    {
        private const string InsertBook =
            "INSERT INTO books (title, isbn, publication_year) VALUES (@title, @isbn, @year)";
        private const string InsertUser =
            "INSERT INTO users (full_name, email, password_hash) VALUES (@name, @email, @hash)";

        public SchemaConstraintTests(PostgresServer server) : base(server)
        {
        }

        [Fact]
        public async Task SchemaScript_RunsSecondTime_WithoutErrorsAndDuplicates()
        {
            // Act: повторний запуск скрипта на базі з даними
            await Db.RunSchemaAsync();

            // Assert: тестові дані не задублювалися
            Assert.Equal(SeedData.UsersCount, await Db.ScalarAsync<long>("SELECT count(*) FROM users"));
            Assert.Equal(SeedData.BooksCount, await Db.ScalarAsync<long>("SELECT count(*) FROM books"));
        }

        [Theory]
        [MemberData(nameof(ValidIsbns), MemberType = typeof(ConstraintTestData))]
        public async Task Book_ValidIsbn_IsSaved(string isbn)
        {
            // Act
            await Db.ExecuteAsync(InsertBook, ("title", BookTitle), ("isbn", isbn), ("year", ValidYear));

            // Assert
            Assert.Equal(1L, await Db.ScalarAsync<long>("SELECT count(*) FROM books WHERE isbn = @isbn", ("isbn", isbn)));
        }

        [Theory]
        [MemberData(nameof(InvalidIsbns), MemberType = typeof(ConstraintTestData))]
        public async Task Book_InvalidIsbn_IsRejectedByDomain(string isbn)
        {
            // Act
            var ex = await Assert.ThrowsAsync<PostgresException>(
                () => Db.ExecuteAsync(InsertBook, ("title", BookTitle), ("isbn", isbn), ("year", ValidYear)));

            // Assert
            Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        }

        [Theory]
        [MemberData(nameof(BoundaryYears), MemberType = typeof(ConstraintTestData))]
        public async Task Book_BoundaryPublicationYear_IsSaved(short year)
        {
            // Act
            await Db.ExecuteAsync(InsertBook, ("title", BookTitle), ("isbn", DBNull.Value), ("year", year));

            // Assert
            Assert.Equal(1L, await Db.ScalarAsync<long>("SELECT count(*) FROM books WHERE publication_year = @year", ("year", year)));
        }

        [Theory]
        [MemberData(nameof(InvalidYears), MemberType = typeof(ConstraintTestData))]
        public async Task Book_PublicationYearOutOfRange_IsRejectedByDomain(short year)
        {
            // Act
            var ex = await Assert.ThrowsAsync<PostgresException>(
                () => Db.ExecuteAsync(InsertBook, ("title", BookTitle), ("isbn", DBNull.Value), ("year", year)));

            // Assert
            Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        }

        [Theory]
        [MemberData(nameof(InvalidEmails), MemberType = typeof(ConstraintTestData))]
        public async Task User_InvalidEmail_IsRejectedByDomain(string email)
        {
            // Act
            var ex = await Assert.ThrowsAsync<PostgresException>(
                () => Db.ExecuteAsync(InsertUser, ("name", UserName), ("email", email), ("hash", PasswordHash)));

            // Assert
            Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        }

        [Theory]
        [MemberData(nameof(ExistingEmailVariants), MemberType = typeof(ConstraintTestData))]
        public async Task User_SameEmailInDifferentCase_IsRejectedByUniqueIndex(string email)
        {
            // Act
            var ex = await Assert.ThrowsAsync<PostgresException>(
                () => Db.ExecuteAsync(InsertUser, ("name", UserName), ("email", email), ("hash", PasswordHash)));

            // Assert: індекс ux_users_email побудований на lower(email)
            Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
            Assert.Equal("ux_users_email", ex.ConstraintName);
        }

        [Fact]
        public async Task BookCopy_WrittenOffWithoutDate_IsRejected()
        {
            // Act
            var ex = await Assert.ThrowsAsync<PostgresException>(() => Db.ExecuteAsync(
                "UPDATE book_copies SET status = 'written_off' WHERE inventory_number = @inv",
                ("inv", SeedData.AvailableInventory)));

            // Assert
            Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
            Assert.Equal("ck_writeoff_consistent", ex.ConstraintName);
        }
    }
}
