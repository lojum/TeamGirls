using DataAccess;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardSnapshot> GetSnapshotAsync();
}

public sealed record OverdueLoanSummary(string ReaderName, string BookTitle, int DaysOverdue);
public sealed record CatalogBookSummary(string Title, string Authors, short PublicationYear, int CopiesCount);
public sealed record ReaderSummary(string FullName, string Email, string Status, int ActiveLoans);

public sealed record AdminDashboardSnapshot(
    int TotalBooks,
    int ActiveReaders,
    int OverdueItems,
    IReadOnlyList<OverdueLoanSummary> OverdueLoans,
    IReadOnlyList<CatalogBookSummary> Catalog,
    IReadOnlyList<ReaderSummary> Readers);

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly SmartLibDbContext _context;

    public AdminDashboardService(SmartLibDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardSnapshot> GetSnapshotAsync()
    {
        var today = DateTime.UtcNow.Date;
        var totalBooks = await _context.Books.AsNoTracking().CountAsync();
        var activeReaders = await _context.Users.AsNoTracking()
            .CountAsync(user => user.Role == UserRole.Reader && user.IsActive);
        var overdueLoans = await _context.Loans.AsNoTracking()
            .Where(loan => loan.ReturnedAt == null && loan.DueDate < today)
            .Select(loan => new
            {
                ReaderName = loan.Reader.FullName,
                BookTitle = loan.Copy.Book.Title,
                loan.DueDate
            })
            .ToListAsync();

        var catalog = await _context.Books.AsNoTracking()
            .Include(book => book.Authors)
            .Include(book => book.Copies)
            .OrderBy(book => book.Title)
            .ToListAsync();

        var readers = await _context.Users.AsNoTracking()
            .Where(user => user.Role == UserRole.Reader)
            .OrderBy(user => user.FullName)
            .Select(user => new ReaderSummary(
                user.FullName,
                user.Email,
                user.IsActive ? "Active" : "Inactive",
                user.LoansAsReader.Count(loan => loan.ReturnedAt == null)))
            .ToListAsync();

        return new AdminDashboardSnapshot(
            totalBooks,
            activeReaders,
            overdueLoans.Count,
            overdueLoans.Select(loan => new OverdueLoanSummary(
                loan.ReaderName,
                loan.BookTitle,
                Math.Max(0, (today - loan.DueDate.Date).Days))).ToList(),
            catalog.Select(book => new CatalogBookSummary(
                book.Title,
                string.Join(", ", book.Authors.Select(author => author.FullName)),
                book.PublicationYear,
                book.Copies.Count)).ToList(),
            readers);
    }
}
