using Domain.Entities;

namespace SmartLib.Tests.TestData
{
    public static class BookTestData
    {
        // Каталог для тестів пошуку; щоразу новий список, щоб тести не впливали один на одного
        public static List<BookCatalogView> Catalog() => new()
        {
            new() { BookId = 1, Title = "Кобзар", Authors = "Тарас Шевченко", Genres = "Класична література, Поезія", PublicationYear = 2020 },
            new() { BookId = 2, Title = "Захар Беркут", Authors = "Іван Франко", Genres = "Класична література", PublicationYear = 2019 },
            new() { BookId = 3, Title = "1984", Authors = "Джордж Орвелл", Genres = "Антиутопія", PublicationYear = 2021 },
            new() { BookId = 4, Title = "Без автора", Authors = "", Genres = "", PublicationYear = 2018, ISBN = null, Publisher = null }
        };

        // Запити, які мають знаходити «Кобзар»: інший регістр, частина слова, пробіли
        public static TheoryData<string> KobzarQueries => new()
        {
            "кобзар",
            "КОБЗАР",
            "обза",
            "  Кобзар  "
        };

        public static TheoryData<string?> EmptyQueries => new()
        {
            null,
            "",
            "   "
        };

        // Запити без збігів, серед них спецсимволи та спроба SQL-ін'єкції
        public static TheoryData<string> NoMatchQueries => new()
        {
            "Гаррі Поттер",
            "%",
            "'; DROP TABLE books; --"
        };
    }
}
