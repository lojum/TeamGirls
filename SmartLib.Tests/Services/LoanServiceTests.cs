using BusinessLogic.Services;
using DataAccess;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SmartLib.Tests.Helpers;
using SmartLib.Tests.TestData;
using static SmartLib.Tests.TestData.LoanTestData;

namespace SmartLib.Tests.Services
{
    [Trait("Category", "Unit")]
    public class LoanServiceTests
    {
        private static (SmartLibDbContext Db, LoanService Service, BookCopy Copy) Arrange(
            CopyStatus copyStatus = CopyStatus.Available, bool readerActive = true)
        {
            var db = TestDb.Create();
            TestDb.AddUser(db, UserTestData.ReaderEmail, isActive: readerActive);
            var book = TestDb.AddBook(db, BookTitle, BookAuthor, BookGenre);
            var copy = TestDb.AddCopy(db, book, InventoryNumber, copyStatus);
            return (db, new LoanService(db, new FixedTimeProvider(Now)), copy);
        }

        [Fact]
        public async Task IssueBook_AvailableCopy_CreatesLoanWithDueDateIn14Days()
        {
            // Arrange
            var (db, service, copy) = Arrange();

            // Act
            var loan = await service.IssueBookAsync(InventoryNumber, UserTestData.ReaderEmail, LibrarianId);

            // Assert
            var saved = await db.Loans.SingleAsync();
            Assert.Equal(loan.LoanId, saved.LoanId);
            Assert.Equal(copy.CopyId, saved.CopyId);
            Assert.Equal(LibrarianId, saved.LibrarianId);
            Assert.Equal(Now.UtcDateTime, saved.LoanedAt);
            Assert.Equal(ExpectedDueDate, saved.DueDate);
            Assert.Null(saved.ReturnedAt);
        }

        [Fact]
        public async Task IssueBook_Success_RaisesBookLoanedEvent()
        {
            // Arrange
            var (_, service, _) = Arrange();
            LoanEventArgs? raised = null;
            service.BookLoaned += (_, e) => raised = e;

            // Act
            await service.IssueBookAsync(InventoryNumber, UserTestData.ReaderEmail, LibrarianId);

            // Assert
            Assert.NotNull(raised);
            Assert.Equal(BookTitle, raised.BookTitle);
            Assert.Equal(UserTestData.ReaderName, raised.ReaderName);
        }

        [Fact]
        public async Task IssueBook_EmailInDifferentCase_FindsReader()
        {
            // Arrange
            var (db, service, _) = Arrange();

            // Act
            await service.IssueBookAsync($" {InventoryNumber} ", "  OLENA.B@gmail.com ", LibrarianId);

            // Assert
            Assert.Equal(1, await db.Loans.CountAsync());
        }

        [Fact]
        public async Task IssueBook_UnknownReader_Throws()
        {
            // Arrange
            var (_, service, _) = Arrange();

            // Act
            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(InventoryNumber, UserTestData.UnknownEmail, LibrarianId));

            // Assert
            Assert.Contains(ReaderNotFoundMessage, ex.Message);
        }

        [Fact]
        public async Task IssueBook_EmailOfLibrarian_IsNotTreatedAsReader()
        {
            // Arrange
            var (db, service, _) = Arrange();
            TestDb.AddUser(db, UserTestData.LibrarianEmail, UserRole.Librarian);

            // Act & Assert
            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(InventoryNumber, UserTestData.LibrarianEmail, LibrarianId));
        }

        [Fact]
        public async Task IssueBook_InactiveReader_Throws()
        {
            // Arrange
            var (db, service, _) = Arrange(readerActive: false);

            // Act
            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(InventoryNumber, UserTestData.ReaderEmail, LibrarianId));

            // Assert
            Assert.Contains(InactiveReaderMessage, ex.Message);
            Assert.Empty(db.Loans);
        }

        [Fact]
        public async Task IssueBook_UnknownInventoryNumber_Throws()
        {
            // Arrange
            var (_, service, _) = Arrange();

            // Act
            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(UnknownInventoryNumber, UserTestData.ReaderEmail, LibrarianId));

            // Assert
            Assert.Contains(CopyNotFoundMessage, ex.Message);
        }

        [Theory]
        [MemberData(nameof(UnavailableStatuses), MemberType = typeof(LoanTestData))]
        public async Task IssueBook_CopyNotAvailable_Throws(CopyStatus status)
        {
            // Arrange
            var (db, service, _) = Arrange(status);

            // Act & Assert
            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(InventoryNumber, UserTestData.ReaderEmail, LibrarianId));
            Assert.Empty(db.Loans);
        }

        [Fact]
        public async Task IssueBook_SameCopyTwice_SecondCallThrows()
        {
            // Arrange
            var (db, service, _) = Arrange();
            await service.IssueBookAsync(InventoryNumber, UserTestData.ReaderEmail, LibrarianId);

            // Act & Assert
            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(InventoryNumber, UserTestData.ReaderEmail, LibrarianId));
            Assert.Equal(1, await db.Loans.CountAsync());
        }

        [Fact]
        public async Task IssueBook_Failure_DoesNotRaiseEvent()
        {
            // Arrange
            var (_, service, _) = Arrange(CopyStatus.WrittenOff);
            var raised = false;
            service.BookLoaned += (_, _) => raised = true;

            // Act
            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(InventoryNumber, UserTestData.ReaderEmail, LibrarianId));

            // Assert
            Assert.False(raised);
        }

        [Theory]
        [MemberData(nameof(EmptyInputs), MemberType = typeof(LoanTestData))]
        public async Task IssueBook_EmptyInput_Throws(string inventoryNumber, string email)
        {
            // Arrange
            var (_, service, _) = Arrange();

            // Act & Assert
            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(inventoryNumber, email, LibrarianId));
        }

        [Theory]
        [MemberData(nameof(CustomDurations), MemberType = typeof(LoanTestData))]
        public async Task IssueBook_CustomDuration_SetsDueDate(int loanDays, DateTime expectedDueDate)
        {
            // Arrange
            var (db, service, _) = Arrange();

            // Act
            await service.IssueBookAsync(InventoryNumber, UserTestData.ReaderEmail, LibrarianId, loanDays);

            // Assert
            Assert.Equal(expectedDueDate, (await db.Loans.SingleAsync()).DueDate);
        }

        [Theory]
        [MemberData(nameof(InvalidDurations), MemberType = typeof(LoanTestData))]
        public async Task IssueBook_InvalidDuration_Throws(int loanDays)
        {
            // Arrange
            var (db, service, _) = Arrange();

            // Act
            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(InventoryNumber, UserTestData.ReaderEmail, LibrarianId, loanDays));

            // Assert
            Assert.Contains(InvalidDurationMessage, ex.Message);
            Assert.Empty(db.Loans);
        }

        [Fact]
        public async Task GetActiveLoans_ReturnsOnlyReadersUnreturnedLoans()
        {
            // Arrange: видача читача, повернена видача та видача іншого читача
            var (db, service, copy) = Arrange();
            var reader = await db.Users.SingleAsync();
            var other = TestDb.AddUser(db, LibrarianTestData.SecondReaderEmail);
            var book = await db.Books.SingleAsync();
            var returnedCopy = TestDb.AddCopy(db, book, LibrarianTestData.LoanedInventory);
            var otherCopy = TestDb.AddCopy(db, book, LibrarianTestData.WrittenOffInventory);
            TestDb.AddLoan(db, copy, reader, Now.UtcDateTime, ExpectedDueDate);
            TestDb.AddLoan(db, returnedCopy, reader, Now.UtcDateTime, ExpectedDueDate, Now.UtcDateTime.AddDays(1));
            TestDb.AddLoan(db, otherCopy, other, Now.UtcDateTime, ExpectedDueDate);

            // Act
            var loans = await service.GetActiveLoansAsync(reader.UserId);

            // Assert
            var loan = Assert.Single(loans);
            Assert.Equal(InventoryNumber, loan.InventoryNumber);
            Assert.Equal(BookTitle, loan.BookTitle);
            Assert.Equal(book.BookId, loan.BookId);
            Assert.Equal(ExpectedDueDate, loan.DueDate);
        }
    }
}
