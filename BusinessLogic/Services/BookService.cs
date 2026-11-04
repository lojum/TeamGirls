using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        public BookService(SmartLibDbContext context)
        {
            _context = context;
        }

        public async Task<List<BookCatalogView>> SearchBooksAsync(string query)
        {
            return await ApplySearch(_context.BookCatalogViews, query).AsNoTracking().ToListAsync();
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