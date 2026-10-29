using DataAccess;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SmartLib.Tests.DataAccess
{
    // Перевіряємо, що модель EF збігається зі schema.sql.
    // Підключення до бази не потрібне: будується лише модель з провайдером Npgsql.
    public class DbContextMappingTests
    {
        private static SmartLibDbContext CreateNpgsqlContext() =>
            new(new DbContextOptionsBuilder<SmartLibDbContext>()
                .UseNpgsql("Host=localhost;Database=model_only")
                .UseSnakeCaseNamingConvention()
                .Options);

        private static IEnumerable<string> ColumnsOf(IEntityType entity)
        {
            var table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            return entity.GetProperties().Select(p => p.GetColumnName(table)!);
        }

        [Theory]
        [InlineData("book_authors", "book_id", "author_id")]
        [InlineData("book_genres", "book_id", "genre_id")]
        public void JoinTables_UseColumnNamesFromSchema(string table, string bookColumn, string otherColumn)
        {
            using var db = CreateNpgsqlContext();

            var entity = db.Model.GetEntityTypes().Single(e => e.GetTableName() == table);

            Assert.Equal(new[] { bookColumn, otherColumn }.Order(), ColumnsOf(entity).Order());
        }

        [Theory]
        [InlineData(typeof(Loan), nameof(Loan.DueDate))]
        [InlineData(typeof(BookCopy), nameof(BookCopy.AcquiredAt))]
        [InlineData(typeof(BookCopy), nameof(BookCopy.WrittenOffAt))]
        public void DateOnlyColumns_AreMappedAsDate(Type entityType, string property)
        {
            using var db = CreateNpgsqlContext();

            var column = db.Model.FindEntityType(entityType)!.FindProperty(property)!;

            Assert.Equal("date", column.GetColumnType());
        }

        [Theory]
        [InlineData(typeof(User), "users")]
        [InlineData(typeof(Book), "books")]
        [InlineData(typeof(BookCopy), "book_copies")]
        [InlineData(typeof(Loan), "loans")]
        [InlineData(typeof(Reservation), "reservations")]
        public void Entities_MapToSchemaTables(Type entityType, string table)
        {
            using var db = CreateNpgsqlContext();

            Assert.Equal(table, db.Model.FindEntityType(entityType)!.GetTableName());
        }

        [Fact]
        public void BookCatalogView_MapsToViewColumns()
        {
            using var db = CreateNpgsqlContext();

            var entity = db.Model.FindEntityType(typeof(BookCatalogView))!;
            var view = StoreObjectIdentifier.View(entity.GetViewName()!, entity.GetViewSchema());
            var columns = entity.GetProperties().Select(p => p.GetColumnName(view)!);

            Assert.Equal("v_book_catalog", entity.GetViewName());
            Assert.Equal(
                new[] { "authors", "available_copies", "book_id", "genres", "isbn", "publication_year", "publisher", "title", "total_copies" },
                columns.Order());
        }

        [Fact]
        public void Loan_ColumnsMatchSchema()
        {
            using var db = CreateNpgsqlContext();

            var columns = ColumnsOf(db.Model.FindEntityType(typeof(Loan))!);

            Assert.Equal(
                new[] { "copy_id", "due_date", "librarian_id", "loan_id", "loaned_at", "reader_id", "returned_at" },
                columns.Order());
        }
    }
}
