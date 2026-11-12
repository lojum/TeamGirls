using Domain.Entities;

namespace SmartLib.Tests.TestData
{
    public static class SchemaTestData
    {
        public static TheoryData<string, string, string> JoinTables => new()
        {
            { "book_authors", "book_id", "author_id" },
            { "book_genres", "book_id", "genre_id" }
        };

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
            { typeof(Reservation), "reservations" },
            { typeof(Fine), "fines" },
            { typeof(Location), "locations" }
        };

        public const string CatalogViewName = "v_book_catalog";

        public static readonly string[] BookCatalogViewColumns =
        {
            "authors", "available_copies", "book_id", "genres", "isbn",
            "publication_year", "publisher", "title", "total_copies"
        };

        public static TheoryData<Type, string[]> EntityColumns => new()
        {
            { typeof(Loan), new[] { "copy_id", "due_date", "librarian_id", "loan_id", "loaned_at", "reader_id", "returned_at" } },
            { typeof(BookCopy), new[] { "acquired_at", "book_id", "copy_id", "inventory_number", "location_id", "status", "write_off_reason", "written_off_at" } },
            { typeof(Fine), new[] { "amount", "created_at", "fine_id", "is_paid", "loan_id", "paid_at" } },
            { typeof(Location), new[] { "location_id", "room", "shelf" } }
        };
    }
}
