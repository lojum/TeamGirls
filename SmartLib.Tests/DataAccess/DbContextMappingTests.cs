using DataAccess;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SmartLib.Tests.TestData;

namespace SmartLib.Tests.DataAccess
{
    // Перевіряємо, що модель EF збігається зі schema.sql.
    // Підключення до бази не потрібне: будується лише модель з провайдером Npgsql.
    [Trait("Category", "Mapping")]
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
        [MemberData(nameof(SchemaTestData.JoinTables), MemberType = typeof(SchemaTestData))]
        public void JoinTables_UseColumnNamesFromSchema(string table, string bookColumn, string otherColumn)
        {
            using var db = CreateNpgsqlContext();

            var entity = db.Model.GetEntityTypes().Single(e => e.GetTableName() == table);

            Assert.Equal(new[] { bookColumn, otherColumn }.Order(), ColumnsOf(entity).Order());
        }

        [Theory]
        [MemberData(nameof(SchemaTestData.DateColumns), MemberType = typeof(SchemaTestData))]
        public void DateOnlyColumns_AreMappedAsDate(Type entityType, string property)
        {
            using var db = CreateNpgsqlContext();

            var column = db.Model.FindEntityType(entityType)!.FindProperty(property)!;

            Assert.Equal("date", column.GetColumnType());
        }

        [Theory]
        [MemberData(nameof(SchemaTestData.EntityTables), MemberType = typeof(SchemaTestData))]
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

            Assert.Equal(SchemaTestData.CatalogViewName, entity.GetViewName());
            Assert.Equal(SchemaTestData.BookCatalogViewColumns, columns.Order());
        }

        [Fact]
        public void Loan_ColumnsMatchSchema()
        {
            using var db = CreateNpgsqlContext();

            var columns = ColumnsOf(db.Model.FindEntityType(typeof(Loan))!);

            Assert.Equal(SchemaTestData.LoanColumns, columns.Order());
        }
    }
}
