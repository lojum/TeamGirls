using DataAccess;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Services;

public interface ILibrarianDashboardService
{
    Task<IReadOnlyList<CopySummary>> GetCopiesAsync();
    Task<IReadOnlyList<ReaderOption>> GetActiveReadersAsync();
    Task<IReadOnlyList<LoanActivity>> GetRecentActivityAsync(int count = LibrarianDashboardService.RecentActivityCount);
}

// Примірник книги для таблиці бібліотекаря; DueDate заповнений, якщо примірник виданий
public sealed record CopySummary(
    long CopyId,
    string InventoryNumber,
    string Title,
    string Authors,
    string? Isbn,
    string Genres,
    CopyStatus Status,
    DateTime? DueDate);

public sealed record ReaderOption(long UserId, string FullName, string Email)
{
    public string Display => $"{FullName} ({Email})";
}

public sealed record LoanActivity(DateTime Time, string ReaderName, string BookTitle, string InventoryNumber, bool IsReturned);

public sealed class LibrarianDashboardService : ILibrarianDashboardService
{
    public const int RecentActivityCount = 8;

    private readonly SmartLibDbContext _context;

    public LibrarianDashboardService(SmartLibDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CopySummary>> GetCopiesAsync()
    {
        var copies = await _context.BookCopies.AsNoTracking()
            .Include(c => c.Book).ThenInclude(b => b.Authors)
            .Include(c => c.Book).ThenInclude(b => b.Genres)
            .OrderBy(c => c.Book.Title).ThenBy(c => c.InventoryNumber)
            .ToListAsync();

        var dueDates = await _context.Loans.AsNoTracking()
            .Where(l => l.ReturnedAt == null)
            .Select(l => new { l.CopyId, l.DueDate })
            .ToListAsync();
        var dueByCopy = dueDates
            .GroupBy(l => l.CopyId)
            .ToDictionary(g => g.Key, g => g.Min(l => l.DueDate));

        return copies.Select(c =>
        {
            var isOnLoan = dueByCopy.TryGetValue(c.CopyId, out var dueDate);
            return new CopySummary(
                c.CopyId,
                c.InventoryNumber,
                c.Book.Title,
                string.Join(", ", c.Book.Authors.Select(a => a.FullName)),
                c.Book.Isbn,
                string.Join(", ", c.Book.Genres.Select(g => g.Name)),
                // Активна видача важливіша за збережений статус: його оновлює тригер бази
                isOnLoan ? CopyStatus.OnLoan : c.Status,
                isOnLoan ? dueDate : null);
        }).ToList();
    }

    public async Task<IReadOnlyList<ReaderOption>> GetActiveReadersAsync()
    {
        return await _context.Users.AsNoTracking()
            .Where(u => u.Role == UserRole.Reader && u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new ReaderOption(u.UserId, u.FullName, u.Email))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<LoanActivity>> GetRecentActivityAsync(int count = RecentActivityCount)
    {
        return await _context.Loans.AsNoTracking()
            .OrderByDescending(l => l.ReturnedAt ?? l.LoanedAt)
            .Take(count)
            .Select(l => new LoanActivity(
                l.ReturnedAt ?? l.LoanedAt,
                l.Reader.FullName,
                l.Copy.Book.Title,
                l.Copy.InventoryNumber,
                l.ReturnedAt != null))
            .ToListAsync();
    }

    // Фільтр таблиці: пошук за назвою, автором, ISBN або інвентарним номером і за статусом
    public static IEnumerable<CopySummary> Filter(IEnumerable<CopySummary> copies, string? search, CopyStatus? status)
    {
        if (status is not null)
            copies = copies.Where(c => c.Status == status);

        if (string.IsNullOrWhiteSpace(search))
            return copies;

        var query = search.Trim();
        return copies.Where(c =>
            c.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || c.Authors.Contains(query, StringComparison.OrdinalIgnoreCase)
            || c.InventoryNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (c.Isbn ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase));
    }
}
