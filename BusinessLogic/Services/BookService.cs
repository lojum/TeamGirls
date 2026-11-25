using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Domain.Entities;
using DataAccess;

namespace BusinessLogic.Services
{
    public interface IBookService
    {
        Task<List<BookCatalogView>> SearchBooksAsync(string query);
    }

    public class BookService : IBookService
    {
        private readonly SmartLibDbContext _context;
        private readonly ILogger<BookService> _logger;

        public BookService(SmartLibDbContext context, ILogger<BookService>? logger = null)
        {
            _context = context;
            _logger = logger ?? NullLogger<BookService>.Instance;
        }

        public async Task<List<BookCatalogView>> SearchBooksAsync(string query)
        {
            try
            {
                return await ApplySearch(_context.BookCatalogViews, query).AsNoTracking().ToListAsync();
            }
            catch (Exception exception)
            {
                // Інтерфейс покаже користувачеві зрозуміле повідомлення, а подробиці збою лишаються в журналі
                _logger.LogError(exception, "Failed to load the book catalog (search: {Query})", query);
                throw;
            }
        }

        // Фільтр винесено окремо, щоб його можна було перевірити модульними тестами без бази
        public static IQueryable<BookCatalogView> ApplySearch(IQueryable<BookCatalogView> books, string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return books;

            var lowerQuery = query.Trim().ToLower();

            return books.Where(b =>
                b.Title.ToLower().Contains(lowerQuery) ||
                b.Authors.ToLower().Contains(lowerQuery) ||
                b.Genres.ToLower().Contains(lowerQuery) ||
                b.PublicationYear.ToString() == lowerQuery
            );
        }
    }
}