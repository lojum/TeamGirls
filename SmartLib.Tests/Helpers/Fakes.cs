using BusinessLogic.Services;
using Domain.Entities;
using Presentation.Services;

namespace SmartLib.Tests.Helpers
{
    // Fake LoanService: запам'ятовує виклик і за потреби кидає заданий виняток
    public sealed class FakeLoanService : ILoanService
    {
        public event BookLoanedEventHandler? BookLoaned;

        public Exception? ExceptionToThrow { get; set; }
        public List<ReaderLoanSummary> ActiveLoans { get; } = new();
        public (string InventoryNumber, string ReaderEmail, long LibrarianId, int LoanDays)? LastIssue { get; private set; }

        public Task<Loan> IssueBookAsync(string inventoryNumber, string readerEmail, long librarianId,
            int loanDays = LoanService.LoanPeriodDays)
        {
            LastIssue = (inventoryNumber, readerEmail, librarianId, loanDays);
            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            var dueDate = new DateTime(2026, 11, 11).AddDays(loanDays - LoanService.LoanPeriodDays);
            BookLoaned?.Invoke(this, new LoanEventArgs { BookTitle = "Кобзар", ReaderName = "Олена Бондаренко", DueDate = dueDate });
            return Task.FromResult(new Loan { DueDate = dueDate });
        }

        public Task<IReadOnlyList<ReaderLoanSummary>> GetActiveLoansAsync(long readerId) =>
            Task.FromResult<IReadOnlyList<ReaderLoanSummary>>(ActiveLoans);
    }

    // Stub панелі бібліотекаря: повертає заздалегідь підготовлені дані
    public sealed class FakeLibrarianDashboardService : ILibrarianDashboardService
    {
        public List<CopySummary> Copies { get; set; } = new();
        public List<ReaderOption> Readers { get; set; } = new();
        public List<LoanActivity> Activity { get; set; } = new();
        public bool ThrowOnLoad { get; set; }
        public int LoadCount { get; private set; }

        public Task<IReadOnlyList<CopySummary>> GetCopiesAsync()
        {
            LoadCount++;
            if (ThrowOnLoad)
                throw new InvalidOperationException("Database is unavailable");
            return Task.FromResult<IReadOnlyList<CopySummary>>(Copies);
        }

        public Task<IReadOnlyList<ReaderOption>> GetActiveReadersAsync() =>
            Task.FromResult<IReadOnlyList<ReaderOption>>(Readers);

        public Task<IReadOnlyList<LoanActivity>> GetRecentActivityAsync(int count = LibrarianDashboardService.RecentActivityCount) =>
            Task.FromResult<IReadOnlyList<LoanActivity>>(Activity.Take(count).ToList());
    }

    public sealed class FakeNavigationService : IWindowNavigationService
    {
        public bool MainShown { get; private set; }

        public void ShowMain() => MainShown = true;
        public void ShowLogin() { }
        public void ShowRegistration() { }
        public void ShowAdminDashboard(string adminName) { }
        public void ShowLibrarianDashboard(string librarianName, long librarianId) { }
        public void ShowReaderDashboard(string readerName, long readerId) { }
    }
}
