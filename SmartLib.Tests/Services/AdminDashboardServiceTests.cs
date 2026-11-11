using BusinessLogic.Services;
using DataAccess;
using Domain.Enums;
using SmartLib.Tests.Helpers;
using SmartLib.Tests.TestData;
using static SmartLib.Tests.TestData.ServiceTestData;

namespace SmartLib.Tests.Services
{
    [Trait("Category", "Unit")]
    public class AdminDashboardServiceTests
    {
        // Активний читач із трьома видачами (прострочена, у терміні, повернена), неактивний читач і бібліотекар
        private static SmartLibDbContext ArrangeLibrary()
        {
            var db = TestDb.Create();
            var reader = TestDb.AddUser(db, UserTestData.ReaderEmail);
            TestDb.AddUser(db, LibrarianTestData.InactiveReaderEmail, isActive: false, fullName: LibrarianTestData.SecondReaderName);
            TestDb.AddUser(db, UserTestData.LibrarianEmail, UserRole.Librarian);

            var first = TestDb.AddBook(db, FirstBookTitle, FirstBookAuthor, Genre, PublicationYear);
            var second = TestDb.AddBook(db, SecondBookTitle, SecondBookAuthor, Genre, PublicationYear);
            var overdueCopy = TestDb.AddCopy(db, first, OverdueInventory);
            var activeCopy = TestDb.AddCopy(db, second, ActiveInventory);
            var returnedCopy = TestDb.AddCopy(db, second, ReturnedInventory);

            TestDb.AddLoan(db, overdueCopy, reader, Today.AddDays(-20), Today.AddDays(-DaysOverdue));
            TestDb.AddLoan(db, activeCopy, reader, Today.AddDays(-2), Today.AddDays(DaysUntilDue));
            TestDb.AddLoan(db, returnedCopy, reader, Today.AddDays(-30), Today.AddDays(-10), Today.AddDays(-12));
            return db;
        }

        [Fact]
        public async Task GetSnapshot_CountsBooksActiveReadersAndOverdueLoans()
        {
            // Arrange
            using var db = ArrangeLibrary();

            // Act
            var snapshot = await new AdminDashboardService(db).GetSnapshotAsync();

            // Assert: повернена й ще не прострочена видачі не рахуються
            Assert.Equal(2, snapshot.TotalBooks);
            Assert.Equal(1, snapshot.ActiveReaders);
            Assert.Equal(1, snapshot.OverdueItems);
            var overdue = Assert.Single(snapshot.OverdueLoans);
            Assert.Equal(new OverdueLoanSummary(UserTestData.ReaderName, FirstBookTitle, DaysOverdue), overdue);
        }

        [Fact]
        public async Task GetSnapshot_CatalogIsSortedWithAuthorsAndCopyCounts()
        {
            // Arrange
            using var db = ArrangeLibrary();

            // Act
            var snapshot = await new AdminDashboardService(db).GetSnapshotAsync();

            // Assert
            Assert.Equal(
                new[]
                {
                    new CatalogBookSummary(FirstBookTitle, FirstBookAuthor, PublicationYear, 1),
                    new CatalogBookSummary(SecondBookTitle, SecondBookAuthor, PublicationYear, 2)
                },
                snapshot.Catalog);
        }

        [Fact]
        public async Task GetSnapshot_ReadersHaveStatusAndActiveLoanCount()
        {
            // Arrange
            using var db = ArrangeLibrary();

            // Act
            var snapshot = await new AdminDashboardService(db).GetSnapshotAsync();

            // Assert: лише читачі (без бібліотекаря), за абеткою; повернена видача не активна
            Assert.Equal(
                new[]
                {
                    new ReaderSummary(LibrarianTestData.SecondReaderName, LibrarianTestData.InactiveReaderEmail, "Inactive", 0),
                    new ReaderSummary(UserTestData.ReaderName, UserTestData.ReaderEmail, "Active", 2)
                },
                snapshot.Readers);
        }

        [Fact]
        public async Task GetSnapshot_EmptyLibrary_ReturnsZeros()
        {
            // Arrange
            using var db = TestDb.Create();

            // Act
            var snapshot = await new AdminDashboardService(db).GetSnapshotAsync();

            // Assert
            Assert.Equal(0, snapshot.TotalBooks);
            Assert.Equal(0, snapshot.ActiveReaders);
            Assert.Equal(0, snapshot.OverdueItems);
            Assert.Empty(snapshot.OverdueLoans);
            Assert.Empty(snapshot.Catalog);
            Assert.Empty(snapshot.Readers);
        }
    }
}
