using BusinessLogic.Services;
using Domain.Enums;
using SmartLib.Tests.Helpers;
using SmartLib.Tests.TestData;
using static SmartLib.Tests.TestData.LibrarianTestData;

namespace SmartLib.Tests.Services
{
    [Trait("Category", "Unit")]
    public class LibrarianDashboardServiceTests
    {
        [Fact]
        public async Task GetCopies_CopyWithActiveLoan_IsOnLoanWithDueDate()
        {
            // Arrange: у InMemory немає тригера, тож статус примірника лишається Available
            using var db = TestDb.Create();
            var reader = TestDb.AddUser(db, UserTestData.ReaderEmail);
            var book = TestDb.AddBook(db, LoanTestData.BookTitle, LoanTestData.BookAuthor, LoanTestData.BookGenre);
            var loaned = TestDb.AddCopy(db, book, LoanedInventory);
            TestDb.AddCopy(db, book, AvailableInventory);
            TestDb.AddLoan(db, loaned, reader, LoanedAt, DueDate);

            // Act
            var copies = await new LibrarianDashboardService(db).GetCopiesAsync();

            // Assert
            var onLoan = Assert.Single(copies, c => c.InventoryNumber == LoanedInventory);
            Assert.Equal(CopyStatus.OnLoan, onLoan.Status);
            Assert.Equal(DueDate, onLoan.DueDate);
            var available = Assert.Single(copies, c => c.InventoryNumber == AvailableInventory);
            Assert.Equal(CopyStatus.Available, available.Status);
            Assert.Null(available.DueDate);
            Assert.Equal(LoanTestData.BookAuthor, available.Authors);
            Assert.Equal(LoanTestData.BookGenre, available.Genres);
        }

        [Fact]
        public async Task GetCopies_ReturnedLoan_DoesNotMarkCopyAsOnLoan()
        {
            // Arrange
            using var db = TestDb.Create();
            var reader = TestDb.AddUser(db, UserTestData.ReaderEmail);
            var book = TestDb.AddBook(db, LoanTestData.BookTitle, LoanTestData.BookAuthor, LoanTestData.BookGenre);
            var copy = TestDb.AddCopy(db, book, AvailableInventory);
            TestDb.AddLoan(db, copy, reader, LoanedAt, DueDate, ReturnedAt);

            // Act
            var copies = await new LibrarianDashboardService(db).GetCopiesAsync();

            // Assert
            Assert.Equal(CopyStatus.Available, Assert.Single(copies).Status);
        }

        [Fact]
        public async Task GetActiveReaders_ExcludesInactiveUsersAndStaff()
        {
            // Arrange
            using var db = TestDb.Create();
            TestDb.AddUser(db, UserTestData.ReaderEmail);
            TestDb.AddUser(db, SecondReaderEmail, fullName: SecondReaderName);
            TestDb.AddUser(db, InactiveReaderEmail, isActive: false);
            TestDb.AddUser(db, UserTestData.LibrarianEmail, UserRole.Librarian);

            // Act
            var readers = await new LibrarianDashboardService(db).GetActiveReadersAsync();

            // Assert: лише активні читачі, за абеткою
            Assert.Equal(new[] { SecondReaderEmail, UserTestData.ReaderEmail }, readers.Select(r => r.Email));
        }

        [Fact]
        public async Task GetRecentActivity_NewestFirst_WithIssuedAndReturned()
        {
            // Arrange
            using var db = TestDb.Create();
            var reader = TestDb.AddUser(db, UserTestData.ReaderEmail);
            var book = TestDb.AddBook(db, LoanTestData.BookTitle, LoanTestData.BookAuthor, LoanTestData.BookGenre);
            var returnedCopy = TestDb.AddCopy(db, book, AvailableInventory);
            var loanedCopy = TestDb.AddCopy(db, book, LoanedInventory);
            TestDb.AddLoan(db, returnedCopy, reader, LoanedAt, DueDate, ReturnedAt);
            TestDb.AddLoan(db, loanedCopy, reader, ReturnedAt.AddDays(1), DueDate);

            // Act
            var activity = await new LibrarianDashboardService(db).GetRecentActivityAsync();

            // Assert
            Assert.Equal(2, activity.Count);
            Assert.False(activity[0].IsReturned);
            Assert.Equal(LoanedInventory, activity[0].InventoryNumber);
            Assert.True(activity[1].IsReturned);
            Assert.Equal(ReturnedAt, activity[1].Time);
        }

        [Theory]
        [MemberData(nameof(SearchCases), MemberType = typeof(LibrarianTestData))]
        public void Filter_BySearch_FindsCopy(string search, string expectedInventory)
        {
            // Act
            var result = LibrarianDashboardService.Filter(Copies(), search, status: null);

            // Assert
            Assert.Equal(expectedInventory, Assert.Single(result).InventoryNumber);
        }

        [Theory]
        [MemberData(nameof(StatusCases), MemberType = typeof(LibrarianTestData))]
        public void Filter_ByStatus_ReturnsOnlyThatStatus(CopyStatus status, string expectedInventory)
        {
            // Act
            var result = LibrarianDashboardService.Filter(Copies(), search: null, status);

            // Assert
            Assert.Equal(expectedInventory, Assert.Single(result).InventoryNumber);
        }

        [Fact]
        public void Filter_NoSearchAndNoStatus_ReturnsAllCopies()
        {
            Assert.Equal(Copies().Count, LibrarianDashboardService.Filter(Copies(), "   ", null).Count());
        }
    }
}
