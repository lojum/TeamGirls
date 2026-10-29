using BusinessLogic.Services;
using Domain.Entities;

namespace SmartLib.Tests.Services
{
    // Каталог читається з представлення v_book_catalog, якого немає в EF InMemory,
    // тому перевіряємо фільтр BookService.ApplySearch на списку в пам'яті
    public class BookServiceTests
    {
        private static readonly List<BookCatalogView> Catalog = new()
        {
            new() { BookId = 1, Title = "Кобзар", Authors = "Тарас Шевченко", Genres = "Класична література, Поезія", PublicationYear = 2020 },
            new() { BookId = 2, Title = "Захар Беркут", Authors = "Іван Франко", Genres = "Класична література", PublicationYear = 2019 },
            new() { BookId = 3, Title = "1984", Authors = "Джордж Орвелл", Genres = "Антиутопія", PublicationYear = 2021 },
            new() { BookId = 4, Title = "Без автора", Authors = "", Genres = "", PublicationYear = 2018, ISBN = null, Publisher = null }
        };

        private static List<string> Search(string? query) =>
            BookService.ApplySearch(Catalog.AsQueryable(), query).Select(b => b.Title).ToList();

        [Fact]
        public void Search_ByTitle_ReturnsMatchingBook()
        {
            Assert.Equal(new[] { "Кобзар" }, Search("Кобзар"));
        }

        [Fact]
        public void Search_ByAuthor_ReturnsAuthorsBook()
        {
            Assert.Equal(new[] { "Захар Беркут" }, Search("Франко"));
        }

        [Fact]
        public void Search_ByGenre_ReturnsAllBooksOfGenre()
        {
            Assert.Equal(new[] { "Кобзар", "Захар Беркут" }, Search("класична"));
        }

        [Fact]
        public void Search_ByExactYear_ReturnsBookOfThatYear()
        {
            Assert.Equal(new[] { "1984" }, Search("2021"));
        }

        [Fact]
        public void Search_ByPartOfYear_DoesNotMatch()
        {
            // Рік порівнюється повністю, а не частиною
            Assert.Empty(Search("202"));
        }

        [Theory]
        [InlineData("кобзар")]
        [InlineData("КОБЗАР")]
        [InlineData("обза")]
        [InlineData("  Кобзар  ")]
        public void Search_IgnoresCaseSpacesAndMatchesSubstring(string query)
        {
            Assert.Equal(new[] { "Кобзар" }, Search(query));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Search_EmptyQuery_ReturnsAllBooks(string? query)
        {
            Assert.Equal(Catalog.Count, Search(query).Count);
        }

        [Theory]
        [InlineData("Гаррі Поттер")]
        [InlineData("%")]
        [InlineData("'; DROP TABLE books; --")]
        public void Search_NoMatch_ReturnsEmptyList(string query)
        {
            Assert.Empty(Search(query));
        }

        [Fact]
        public void Search_BookWithoutAuthorsAndGenres_IsFoundByTitle()
        {
            Assert.Equal(new[] { "Без автора" }, Search("без"));
        }
    }
}
