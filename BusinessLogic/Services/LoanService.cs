using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;
using DataAccess;

namespace BusinessLogic.Services
{
    public class LoanEventArgs : EventArgs
    {
        public string BookTitle { get; set; } = string.Empty;
        public string ReaderName { get; set; } = string.Empty;
    }

    public class LoanException : Exception
    {
        public LoanException(string message) : base(message)
        {
        }
    }

    public delegate void BookLoanedEventHandler(object sender, LoanEventArgs e);

    public interface ILoanService
    {
        event BookLoanedEventHandler BookLoaned;
        Task<Loan> IssueBookAsync(string inventoryNumber, string readerEmail, long librarianId);
    }

    public class LoanService : ILoanService
    {
        public const int LoanPeriodDays = 14;

        private readonly SmartLibDbContext _context;
        private readonly TimeProvider _timeProvider;

        public event BookLoanedEventHandler? BookLoaned;

        public LoanService(SmartLibDbContext context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        public async Task<Loan> IssueBookAsync(string inventoryNumber, string readerEmail, long librarianId)
        {
            if (string.IsNullOrWhiteSpace(inventoryNumber))
                throw new LoanException("Enter the item's inventory number.");

            if (string.IsNullOrWhiteSpace(readerEmail))
                throw new LoanException("Enter the reader's email.");

            var email = AuthService.NormalizeEmail(readerEmail);
            var reader = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Role == UserRole.Reader);

            if (reader == null)
                throw new LoanException("Reader with this email was not found.");

            if (!reader.IsActive)
                throw new LoanException("Reader account is inactive.");

            var copy = await _context.BookCopies
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.InventoryNumber == inventoryNumber.Trim());

            if (copy == null)
                throw new LoanException("No book copy found with that inventory number.");

            var hasActiveLoan = await _context.Loans
                .AnyAsync(l => l.CopyId == copy.CopyId && l.ReturnedAt == null);

            if (copy.Status != CopyStatus.Available || hasActiveLoan)
                throw new LoanException("Copy with this inventory number is not available for checkout.");

            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var loan = new Loan
            {
                CopyId = copy.CopyId,
                ReaderId = reader.UserId,
                LibrarianId = librarianId,
                LoanedAt = now,
                DueDate = now.Date.AddDays(LoanPeriodDays)
            };

            _context.Loans.Add(loan);

            await _context.SaveChangesAsync();

            BookLoaned?.Invoke(this, new LoanEventArgs
            {
                BookTitle = copy.Book.Title,
                ReaderName = reader.FullName
            });

            return loan;
        }
    }
}
