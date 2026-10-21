using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using DataAccess;

namespace BusinessLogic.Services
{
    // Інтерфейс для ін'єкції залежностей
    public interface IBookService
    {
        Task<List<Book>> SearchBooksAsync(string query);
    }

    public class BookService : IBookService
    {
        private readonly SmartLibDbContext _context;

        public BookService(SmartLibDbContext context)
        {
            _context = context;
        }

        public async Task<List<Book>> SearchBooksAsync(string query)
        {
            // Отримуємо книги разом із їхніми авторами та жанрами (для таблиці)
            var booksQuery = _context.Books
                .Include(b => b.Authors)
                .Include(b => b.Genres)
                .AsQueryable();

            // Якщо запит порожній - повертаємо всі книги. Якщо ні - фільтруємо.
            if (!string.IsNullOrWhiteSpace(query))
            {
                var lowerQuery = query.ToLower();

                booksQuery = booksQuery.Where(b => 
                    b.Title.ToLower().Contains(lowerQuery) || 
                    b.Authors.Any(a => a.FullName.ToLower().Contains(lowerQuery)) ||
                    b.Genres.Any(g => g.Name.ToLower().Contains(lowerQuery))
                );
            }

            // Повертаємо результат швидко (до 2 секунд) завдяки AsNoTracking
            return await booksQuery.AsNoTracking().ToListAsync();
        }
    }
}