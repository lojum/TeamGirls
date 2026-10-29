using DataAccess;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace SmartLib.Tests.Helpers
{
    // Окрема in-memory база для кожного тесту, щоб тести не впливали один на одного
    public static class TestDb
    {
        public const string ReaderPassword = "Reader123!";

        public static SmartLibDbContext Create() =>
            new(new DbContextOptionsBuilder<SmartLibDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        public static User AddUser(SmartLibDbContext db, string email, UserRole role = UserRole.Reader,
            bool isActive = true, string password = ReaderPassword, string fullName = "Олена Бондаренко")
        {
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BusinessLogic.Services.PasswordHasher.Hash(password),
                Role = role,
                IsActive = isActive
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user;
        }

        public static Book AddBook(SmartLibDbContext db, string title, string author, string genre,
            short year = 2020)
        {
            var book = new Book
            {
                Title = title,
                PublicationYear = year,
                Authors = { db.Authors.FirstOrDefault(a => a.FullName == author) ?? new Author { FullName = author } },
                Genres = { db.Genres.FirstOrDefault(g => g.Name == genre) ?? new Genre { Name = genre } }
            };
            db.Books.Add(book);
            db.SaveChanges();
            return book;
        }

        public static BookCopy AddCopy(SmartLibDbContext db, Book book, string inventoryNumber,
            CopyStatus status = CopyStatus.Available)
        {
            var copy = new BookCopy
            {
                BookId = book.BookId,
                InventoryNumber = inventoryNumber,
                Status = status,
                AcquiredAt = DateTime.UtcNow.Date
            };
            db.BookCopies.Add(copy);
            db.SaveChanges();
            return copy;
        }
    }

    // Годинник із фіксованим часом для детермінованих тестів дат
    public sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now) => _now = now;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
