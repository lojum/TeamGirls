using System;
using System.Threading.Tasks;
using DataAccess;
using Domain.Entities;
using Domain.Enums;
using BusinessLogic.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace SmartLib.Tests.Integration
{
    [Trait("Category", "Integration")]
    public class SmartLibBusinessIntegrationTests
    {
        private static SmartLibDbContext CreateInMemoryDb() =>
            new(new DbContextOptionsBuilder<SmartLibDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        [Fact]
        public async Task LoanService_IssueBook_PersistsLoanAndUpdatesStatus_Successfully()
        {
            using var db = CreateInMemoryDb();
            
            var reader = new User 
            { 
                FullName = "Шелеп Софія", 
                Email = "sofia@test.com", 
                Role = UserRole.Reader 
            };

            var book = new Book 
            { 
                Title = "1984" 
            };

            var copy = new BookCopy 
            { 
                Book = book, 
                InventoryNumber = "INV-1984-01", 
                Status = CopyStatus.Available 
            };

            db.Users.Add(reader);
            db.Books.Add(book);
            db.BookCopies.Add(copy);
            await db.SaveChangesAsync();

            var loanService = new LoanService(db);

            bool result = await loanService.IssueBookAsync("INV-1984-01", "sofia@test.com", librarianId: 1);

            Assert.True(result);

            var savedLoan = await db.Loans
                .Include(l => l.Copy)
                .ThenInclude(c => c.Book)
                .Include(l => l.Reader)
                .FirstOrDefaultAsync(l => l.CopyId == copy.CopyId);

            Assert.NotNull(savedLoan);
            Assert.Equal("Шелеп Софія", savedLoan.Reader.FullName);
            Assert.Equal("1984", savedLoan.Copy.Book.Title);
        }

        [Fact]
        public async Task LoanService_IssueBook_ThrowsException_WhenReaderNotFound()
        {
            using var db = CreateInMemoryDb();
            var loanService = new LoanService(db);

            var exception = await Assert.ThrowsAsync<Exception>(() => 
                loanService.IssueBookAsync("ANY-INVENTORY", "nonexistent@test.com", librarianId: 1));

            Assert.Contains("Читача з таким Email не знайдено", exception.Message);
        }

        [Fact]
        public async Task LoanService_FiresBookLoanedEvent_WhenSuccessfullyIssued()
        {
            using var db = CreateInMemoryDb();

            var reader = new User 
            { 
                FullName = "Софія", 
                Email = "sofia@test.com", 
                Role = UserRole.Reader 
            };

            var book = new Book { Title = "Dune" };
            var copy = new BookCopy { Book = book, InventoryNumber = "INV-DUNE-01", Status = CopyStatus.Available };

            db.Users.Add(reader);
            db.Books.Add(book);
            db.BookCopies.Add(copy);
            await db.SaveChangesAsync();

            var loanService = new LoanService(db);
            bool eventFired = false;
            string receivedBookTitle = string.Empty;

            loanService.BookLoaned += (sender, args) =>
            {
                eventFired = true;
                receivedBookTitle = args.BookTitle;
            };

            await loanService.IssueBookAsync("INV-DUNE-01", "sofia@test.com", librarianId: 1);

            Assert.True(eventFired);
            Assert.Equal("Dune", receivedBookTitle);
        }
    }
}