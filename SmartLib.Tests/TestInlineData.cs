using System.Collections.Generic;

namespace SmartLib.Tests.TestData
{
    public static class ComprehensiveTestData
    {
        public static IEnumerable<object[]> IsbnTestData => new List<object[]>
        {
            new object[] { "978-966-00-0000-1", true },
            new object[] { "9789660000001", true },
            new object[] { "978-3-16-148410-0", true },
            new object[] { "123456789", false },
            new object[] { "978-966-00-0000-X", false },
            new object[] { "", false }
        };

        public static IEnumerable<object[]> FineCalculationTestData => new List<object[]>
        {
            new object[] { 1, 10.0m, 10.0m },
            new object[] { 5, 12.5m, 62.5m },
            new object[] { 0, 15.0m, 0.0m }
        };

        public static IEnumerable<object[]> AuthorNameTestData => new List<object[]>
        {
            new object[] { "Іван", "Франко", "Франко І." },
            new object[] { "Леся", "Українка", "Українка Л." },
            new object[] { "John", "Doe", "Doe J." }
        };

        public static IEnumerable<object[]> BookCopyStockTestData => new List<object[]>
        {
            new object[] { 0, false },
            new object[] { 1, true },
            new object[] { 100, true },
            new object[] { -5, false }
        };
    }
}