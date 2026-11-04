using BusinessLogic.Services;
using SmartLib.Tests.TestData;

namespace SmartLib.Tests.Services
{
    // Каталог читається з представлення v_book_catalog, якого немає в EF InMemory,
    // тому перевіряємо фільтр BookService.ApplySearch на списку в пам'яті
    [Trait("Category", "Unit")]
    public class BookServiceTests
    {
        private static List<string> Search(string? query) =>
            BookService.ApplySearch(BookTestData.Catalog().AsQueryable(), query).Select(b => b.Title).ToList();

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
        [MemberData(nameof(BookTestData.KobzarQueries), MemberType = typeof(BookTestData))]
        public void Search_IgnoresCaseSpacesAndMatchesSubstring(string query)
        {
            Assert.Equal(new[] { "Кобзар" }, Search(query));
        }

        [Theory]
        [MemberData(nameof(BookTestData.EmptyQueries), MemberType = typeof(BookTestData))]
        public void Search_EmptyQuery_ReturnsAllBooks(string? query)
        {
            Assert.Equal(BookTestData.Catalog().Count, Search(query).Count);
        }

        [Theory]
        [MemberData(nameof(BookTestData.NoMatchQueries), MemberType = typeof(BookTestData))]
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
