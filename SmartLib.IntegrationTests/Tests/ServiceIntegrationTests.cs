using BusinessLogic.Services;
using SmartLib.IntegrationTests.Infrastructure;
using SmartLib.IntegrationTests.TestData;
using static SmartLib.IntegrationTests.TestData.SeedData;

namespace SmartLib.IntegrationTests.Tests
{
    // Сервіси бізнес-логіки разом зі справжньою базою: запити EF, представлення v_book_catalog, індекси
    [Trait("Category", "Integration")]
    public class ServiceIntegrationTests : IntegrationTestBase
    {
        public ServiceIntegrationTests(PostgresServer server) : base(server)
        {
        }

        [Fact]
        public async Task BookService_SearchByAuthor_ReadsCatalogView()
        {
            // Arrange
            await using var context = Db.CreateContext();

            // Act
            var books = await new BookService(context).SearchBooksAsync("франко");

            // Assert: автори й жанри зібрані представленням, виданий примірник не доступний
            var book = Assert.Single(books);
            Assert.Equal(FrankoBookTitle, book.Title);
            Assert.Equal(FrankoAuthor, book.Authors);
            Assert.Equal(FrankoGenre, book.Genres);
            Assert.Equal(1, book.TotalCopies);
            Assert.Equal(0, book.AvailableCopies);
        }

        [Fact]
        public async Task BookService_EmptyQuery_ReturnsAllBooksWithoutWrittenOffCopies()
        {
            // Arrange
            await using var context = Db.CreateContext();

            // Act
            var books = await new BookService(context).SearchBooksAsync(string.Empty);

            // Assert
            Assert.Equal(BooksCount, books.Count);
            Assert.Equal(0, books.Single(b => b.Title == WrittenOffBookTitle).TotalCopies);
        }

        [Fact]
        public async Task UserService_RegisteredReader_CanLogInWithDifferentCase()
        {
            // Arrange
            await using var context = Db.CreateContext();
            await new UserService(context).RegisterReaderAsync(
                ConstraintTestData.UserName, ConstraintTestData.NewReaderEmail, ConstraintTestData.NewReaderPassword);

            // Act
            var user = await new AuthService(context).AuthenticateAsync(
                ConstraintTestData.NewReaderEmail.ToUpperInvariant(), ConstraintTestData.NewReaderPassword);

            // Assert: email збережено в нижньому регістрі
            Assert.NotNull(user);
            Assert.Equal(ConstraintTestData.NewReaderEmail.ToLowerInvariant(), await Db.ScalarAsync<string>(
                "SELECT email FROM users WHERE user_id = @id", ("id", user.UserId)));
        }

        [Theory]
        [MemberData(nameof(ConstraintTestData.ExistingEmailVariants), MemberType = typeof(ConstraintTestData))]
        public async Task UserService_ExistingEmailInDifferentCase_ThrowsDuplicateEmail(string email)
        {
            // Arrange
            await using var context = Db.CreateContext();

            // Act & Assert
            await Assert.ThrowsAsync<DuplicateEmailException>(() => new UserService(context).RegisterReaderAsync(
                ConstraintTestData.UserName, email, ConstraintTestData.NewReaderPassword));
        }

        [Fact]
        public async Task AdminDashboardService_Snapshot_CountsSeedData()
        {
            // Arrange
            await using var context = Db.CreateContext();

            // Act
            var snapshot = await new AdminDashboardService(context).GetSnapshotAsync();

            // Assert
            Assert.Equal(BooksCount, snapshot.TotalBooks);
            Assert.Equal(ActiveReadersCount, snapshot.ActiveReaders);
            var overdue = Assert.Single(snapshot.OverdueLoans);
            Assert.Equal(OverdueReaderName, overdue.ReaderName);
            Assert.Equal(OverdueBookTitle, overdue.BookTitle);
            Assert.InRange(overdue.DaysOverdue, 5, 7);
        }
    }
}
