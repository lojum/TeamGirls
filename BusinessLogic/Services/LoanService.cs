using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Domain.Entities;
using Domain.Enums;
using DataAccess;
using Npgsql;

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

        public LoanException(string message, Exception innerException) : base(message, innerException)
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

        private const string ActiveLoanIndex = "ux_loans_one_active_per_copy";

        private readonly SmartLibDbContext _context;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<LoanService> _logger;

        public event BookLoanedEventHandler? BookLoaned;

        public LoanService(SmartLibDbContext context, TimeProvider timeProvider, ILogger<LoanService>? logger = null)
        {
            _context = context;
            _timeProvider = timeProvider;
            _logger = logger ?? NullLogger<LoanService>.Instance;
        }

        public async Task<Loan> IssueBookAsync(string inventoryNumber, string readerEmail, long librarianId,
            int loanDays = LoanPeriodDays)
        {
            if (loanDays < 1 || loanDays > MaxLoanDays)
                throw Reject($"Loan duration must be between 1 and {MaxLoanDays} days.");

            if (string.IsNullOrWhiteSpace(inventoryNumber))
                throw Reject("Enter the item's inventory number.");

            if (string.IsNullOrWhiteSpace(readerEmail))
                throw Reject("Enter the reader's email.");

            // Права перевіряємо і в сервісі, а не лише в інтерфейсі: видавати книги може тільки активний бібліотекар
            var isLibrarian = await _context.Users
                .AnyAsync(u => u.UserId == librarianId && u.Role == UserRole.Librarian && u.IsActive);

            if (!isLibrarian)
                throw Reject("Only an active librarian can issue books.");

            var email = AuthService.NormalizeEmail(readerEmail);
            var reader = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Role == UserRole.Reader);

            if (reader == null)
                throw Reject("Reader with this email was not found.");

            if (!reader.IsActive)
                throw Reject("Reader account is inactive.");

            var copy = await _context.BookCopies
                .Include(c => c.Book)
                .FirstOrDefaultAsync(c => c.InventoryNumber == inventoryNumber.Trim());

            if (copy == null)
                throw Reject("No book copy found with that inventory number.");

            var hasActiveLoan = await _context.Loans
                .AnyAsync(l => l.CopyId == copy.CopyId && l.ReturnedAt == null);

            if (copy.Status != CopyStatus.Available || hasActiveLoan)
                throw Reject("Copy with this inventory number is not available for checkout.");

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

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (IsCopyAlreadyTaken(exception))
            {
                // Інший бібліотекар видав цей примірник між нашою перевіркою і збереженням
                _context.Entry(loan).State = EntityState.Detached;
                _logger.LogWarning("Loan rejected: copy {InventoryNumber} was issued by another librarian at the same time",
                    copy.InventoryNumber);
                throw new LoanException(
                    "This copy has just been issued to another reader. Refresh the list and choose another copy.",
                    exception);
            }
            catch (Exception exception)
            {
                _context.Entry(loan).State = EntityState.Detached;
                _logger.LogError(exception, "Failed to save the loan of copy {InventoryNumber}", copy.InventoryNumber);
                throw;
            }

            _logger.LogInformation(
                "Copy {InventoryNumber} issued to reader {ReaderId} by librarian {LibrarianId}, due {DueDate:yyyy-MM-dd}",
                copy.InventoryNumber, reader.UserId, librarianId, loan.DueDate);

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

        // Помилки бізнес-правил записуємо в журнал як попередження: це не збій програми
        private LoanException Reject(string message)
        {
            _logger.LogWarning("Loan rejected: {Reason}", message);
            return new LoanException(message);
        }

        // Унікальний індекс «одна активна видача на примірник» або тригер, що не дає видати недоступний примірник
        private static bool IsCopyAlreadyTaken(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: ActiveLoanIndex
            }
            or PostgresException { SqlState: PostgresErrorCodes.RaiseException };
    }
}
