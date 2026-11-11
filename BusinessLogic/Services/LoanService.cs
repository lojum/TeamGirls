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
        public DateTime DueDate { get; set; }
    }

    public class LoanException : Exception
    {
        public LoanException(string message) : base(message)
        {
        }
    }

    public delegate void BookLoanedEventHandler(object sender, LoanEventArgs e);

    // Активна видача читача для розділу «My Loans»
    public sealed record ReaderLoanSummary(long BookId, string BookTitle, string InventoryNumber, DateTime DueDate);

    public interface ILoanService
    {
        event BookLoanedEventHandler BookLoaned;
        Task<Loan> IssueBookAsync(string inventoryNumber, string readerEmail, long librarianId, int loanDays = LoanService.LoanPeriodDays);
        Task<IReadOnlyList<ReaderLoanSummary>> GetActiveLoansAsync(long readerId);
    }

    public class LoanService : ILoanService
    {
        public const int LoanPeriodDays = 14;
        public const int MaxLoanDays = 60;

        private readonly SmartLibDbContext _context;
        private readonly TimeProvider _timeProvider;

        public event BookLoanedEventHandler? BookLoaned;

        public LoanService(SmartLibDbContext context, TimeProvider timeProvider)
        {
            _context = context;
            _timeProvider = timeProvider;
        }

        public async Task<Loan> IssueBookAsync(string inventoryNumber, string readerEmail, long librarianId,
            int loanDays = LoanPeriodDays)
        {
            if (loanDays < 1 || loanDays > MaxLoanDays)
                throw new LoanException($"Loan duration must be between 1 and {MaxLoanDays} days.");

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
                DueDate = now.Date.AddDays(loanDays)
            };

            _context.Loans.Add(loan);

            await _context.SaveChangesAsync();

            BookLoaned?.Invoke(this, new LoanEventArgs
            {
                BookTitle = copy.Book.Title,
                ReaderName = reader.FullName,
                DueDate = loan.DueDate
            });

            return loan;
        }

        public async Task<IReadOnlyList<ReaderLoanSummary>> GetActiveLoansAsync(long readerId)
        {
            return await _context.Loans.AsNoTracking()
                .Where(l => l.ReaderId == readerId && l.ReturnedAt == null)
                .OrderBy(l => l.DueDate)
                .Select(l => new ReaderLoanSummary(l.Copy.BookId, l.Copy.Book.Title, l.Copy.InventoryNumber, l.DueDate))
                .ToListAsync();
        }
    }
}
