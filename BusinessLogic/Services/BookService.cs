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
            var booksQuery = _context.BookCatalogViews.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var lowerQuery = query.ToLower();
                
                booksQuery = booksQuery.Where(b => 
                    b.Title.ToLower().Contains(lowerQuery) || 
                    b.Authors.ToLower().Contains(lowerQuery) ||
                    b.Genres.ToLower().Contains(lowerQuery) ||
                    b.PublicationYear.ToString() == lowerQuery
                );
            }

            return await booksQuery.AsNoTracking().ToListAsync();
        }
    }
}