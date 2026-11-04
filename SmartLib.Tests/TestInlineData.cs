using Xunit;
using System.Collections.Generic;

namespace SmartLib.Tests
{
    public static class TestData
    {
        public static IEnumerable<object[]> PhoneNumberTestData => new List<object[]>
        {
            new object[] { "+380982330102", true },
            new object[] { "0982330102", true },
            new object[] { "380982330102", true },
            new object[] { "12345", false },
            new object[] { "abcdefghij", false }
        };

        public static IEnumerable<object[]> FineCalculationTestData => new List<object[]>
        {
            new object[] { 0, 5.0m, 0.0m },
            new object[] { 3, 5.0m, 15.0m },
            new object[] { 10, 2.5m, 25.0m }
        };

        public static IEnumerable<object[]> BarcodeTestData => new List<object[]>
        {
            new object[] { "4820000123456", true },
            new object[] { "12345", false },
            new object[] { "482000012345A", false }
        };

        public static IEnumerable<object[]> PermissionsTestData => new List<object[]>
        {
            new object[] { "Admin", true },
            new object[] { "Librarian", true },
            new object[] { "Reader", false }
        };

        public static IEnumerable<object[]> PageCountTestData => new List<object[]>
        {
            new object[] { 150, true },
            new object[] { 0, false },
            new object[] { -50, false }
        };
    }
}