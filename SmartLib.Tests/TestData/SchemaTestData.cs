using Domain.Entities;

namespace SmartLib.Tests.TestData
{
    // Назви таблиць і колонок зі schema.sql, з якими має збігатися модель EF
    public static class SchemaTestData
    {
        public static TheoryData<string, string, string> JoinTables => new()
        {
            { "book_authors", "book_id", "author_id" },
            { "book_genres", "book_id", "genre_id" }
        };

        // Колонки типу DATE (без часу)
        public static TheoryData<Type, string> DateColumns => new()
        {
            { typeof(Loan), nameof(Loan.DueDate) },
            { typeof(BookCopy), nameof(BookCopy.AcquiredAt) },
            { typeof(BookCopy), nameof(BookCopy.WrittenOffAt) }
        };

        public static TheoryData<Type, string> EntityTables => new()
        {
            { typeof(User), "users" },
            { typeof(Book), "books" },
            { typeof(BookCopy), "book_copies" },
            { typeof(Loan), "loans" },
            { typeof(Reservation), "reservations" }
        };

        public const string CatalogViewName = "v_book_catalog";

        public static readonly string[] BookCatalogViewColumns =
        {
            "authors", "available_copies", "book_id", "genres", "isbn",
            "publication_year", "publisher", "title", "total_copies"
        };

        public static readonly string[] LoanColumns =
        {
            "copy_id", "due_date", "librarian_id", "loan_id", "loaned_at", "reader_id", "returned_at"
        };
    }
}
