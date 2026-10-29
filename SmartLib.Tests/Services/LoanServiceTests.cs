using BusinessLogic.Services;
using DataAccess;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SmartLib.Tests.Helpers;

namespace SmartLib.Tests.Services
{
    public class LoanServiceTests
    {
        private const long LibrarianId = 100;
        private const string ReaderEmail = "olena.b@gmail.com";
        private static readonly DateTimeOffset Now = new(2026, 10, 28, 15, 30, 0, TimeSpan.Zero);

        private static (SmartLibDbContext Db, LoanService Service, BookCopy Copy) Arrange(
            CopyStatus copyStatus = CopyStatus.Available, bool readerActive = true)
        {
            var db = TestDb.Create();
            TestDb.AddUser(db, ReaderEmail, isActive: readerActive);
            var book = TestDb.AddBook(db, "Кобзар", "Тарас Шевченко", "Поезія");
            var copy = TestDb.AddCopy(db, book, "INV-00101", copyStatus);
            return (db, new LoanService(db, new FixedTimeProvider(Now)), copy);
        }

        [Fact]
        public async Task IssueBook_AvailableCopy_CreatesLoanWithDueDateIn14Days()
        {
            var (db, service, copy) = Arrange();

            var loan = await service.IssueBookAsync("INV-00101", ReaderEmail, LibrarianId);

            var saved = await db.Loans.SingleAsync();
            Assert.Equal(loan.LoanId, saved.LoanId);
            Assert.Equal(copy.CopyId, saved.CopyId);
            Assert.Equal(LibrarianId, saved.LibrarianId);
            Assert.Equal(Now.UtcDateTime, saved.LoanedAt);
            Assert.Equal(new DateTime(2026, 11, 11), saved.DueDate);
            Assert.Null(saved.ReturnedAt);
        }

        [Fact]
        public async Task IssueBook_Success_RaisesBookLoanedEvent()
        {
            var (_, service, _) = Arrange();
            LoanEventArgs? raised = null;
            service.BookLoaned += (_, e) => raised = e;

            await service.IssueBookAsync("INV-00101", ReaderEmail, LibrarianId);

            Assert.NotNull(raised);
            Assert.Equal("Кобзар", raised.BookTitle);
            Assert.Equal("Олена Бондаренко", raised.ReaderName);
        }

        [Fact]
        public async Task IssueBook_EmailInDifferentCase_FindsReader()
        {
            var (db, service, _) = Arrange();

            await service.IssueBookAsync(" INV-00101 ", "  OLENA.B@gmail.com ", LibrarianId);

            Assert.Equal(1, await db.Loans.CountAsync());
        }

        [Fact]
        public async Task IssueBook_UnknownReader_Throws()
        {
            var (_, service, _) = Arrange();

            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync("INV-00101", "nobody@gmail.com", LibrarianId));
            Assert.Contains("Reader with this email was not found", ex.Message);
        }

        [Fact]
        public async Task IssueBook_EmailOfLibrarian_IsNotTreatedAsReader()
        {
            var (db, service, _) = Arrange();
            TestDb.AddUser(db, "librarian@smartlib.ua", UserRole.Librarian);

            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync("INV-00101", "librarian@smartlib.ua", LibrarianId));
        }

        [Fact]
        public async Task IssueBook_InactiveReader_Throws()
        {
            var (db, service, _) = Arrange(readerActive: false);

            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync("INV-00101", ReaderEmail, LibrarianId));
            Assert.Contains("inactive", ex.Message);
            Assert.Empty(db.Loans);
        }

        [Fact]
        public async Task IssueBook_UnknownInventoryNumber_Throws()
        {
            var (_, service, _) = Arrange();

            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync("INV-99999", ReaderEmail, LibrarianId));
            Assert.Contains("inventory number", ex.Message);
        }

        [Theory]
        [InlineData(CopyStatus.OnLoan)]
        [InlineData(CopyStatus.Reserved)]
        [InlineData(CopyStatus.WrittenOff)]
        public async Task IssueBook_CopyNotAvailable_Throws(CopyStatus status)
        {
            var (db, service, _) = Arrange(status);

            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync("INV-00101", ReaderEmail, LibrarianId));
            Assert.Empty(db.Loans);
        }

        [Fact]
        public async Task IssueBook_SameCopyTwice_SecondCallThrows()
        {
            var (db, service, _) = Arrange();
            await service.IssueBookAsync("INV-00101", ReaderEmail, LibrarianId);

            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync("INV-00101", ReaderEmail, LibrarianId));
            Assert.Equal(1, await db.Loans.CountAsync());
        }

        [Fact]
        public async Task IssueBook_Failure_DoesNotRaiseEvent()
        {
            var (_, service, _) = Arrange(CopyStatus.WrittenOff);
            var raised = false;
            service.BookLoaned += (_, _) => raised = true;

            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync("INV-00101", ReaderEmail, LibrarianId));
            Assert.False(raised);
        }

        [Theory]
        [InlineData("", ReaderEmail)]
        [InlineData("   ", ReaderEmail)]
        [InlineData("INV-00101", "")]
        [InlineData("INV-00101", "   ")]
        public async Task IssueBook_EmptyInput_Throws(string inventoryNumber, string email)
        {
            var (_, service, _) = Arrange();

            await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(inventoryNumber, email, LibrarianId));
        }
    }
}
