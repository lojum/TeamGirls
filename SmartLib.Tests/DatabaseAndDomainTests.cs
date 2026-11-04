using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SmartLib.Tests
{
    public class ExtendedSmartLibTests
    {
        [Theory]
        [MemberData(nameof(TestData.PhoneNumberTestData), MemberType = typeof(TestData))]
        public void User_PhoneNumberValidation_UkrainianFormat(string phone, bool expectedIsValid)
        {
            string cleanPhone = System.Text.RegularExpressions.Regex.Replace(phone, @"[^\d]", "");
            bool isValid = cleanPhone.Length == 10 || (cleanPhone.Length == 12 && cleanPhone.StartsWith("38"));

            Assert.Equal(expectedIsValid, isValid);
        }

        [Theory]
        [MemberData(nameof(TestData.FineCalculationTestData), MemberType = typeof(TestData))]
        public void FineCalculation_OverdueDays_ReturnsCorrectAmount(int overdueDays, decimal dailyRate, decimal expectedFine)
        {
            decimal totalFine = overdueDays * dailyRate;

            Assert.Equal(expectedFine, totalFine);
        }

        [Fact]
        public void Reader_MaxBooksLimit_ExceedingLimitShouldBeBlocked()
        {
            int currentBooksCount = 5;
            int maxAllowedBooks = 5;

            bool canBorrowMore = currentBooksCount < maxAllowedBooks;

            Assert.False(canBorrowMore, "Читач досяг ліміту книг і не може взяти нову.");
        }

        [Theory]
        [MemberData(nameof(TestData.BarcodeTestData), MemberType = typeof(TestData))]
        public void Copy_BarcodeValidation_Test(string barcode, bool expectedIsValid)
        {
            bool isValid = System.Text.RegularExpressions.Regex.IsMatch(barcode, @"^\d{13}$");

            Assert.Equal(expectedIsValid, isValid);
        }

        [Fact]
        public void BookReservation_StatusAvailable_CanBeReserved()
        {
            string copyStatus = "available";
            bool isReserved = false;

            bool canReserve = copyStatus == "available" && !isReserved;

            Assert.True(canReserve, "Доступну та не заброньовану книгу можна забронювати.");
        }

        [Fact]
        public void Membership_ExpirationDate_CheckIfExpired()
        {
            DateTime expiryDate = new DateTime(2025, 1, 1);
            DateTime currentDate = new DateTime(2026, 10, 29);

            bool isExpired = currentDate > expiryDate;

            Assert.True(isExpired, "Термін дії читацького квитка минув.");
        }

        [Fact]
        public void Catalog_FilterByGenre_ReturnsMatchingBooks()
        {
            var books = new List<(string Title, string Genre)>
            {
                ("1984", "Dystopia"),
                ("Dune", "Sci-Fi"),
                ("Brave New World", "Dystopia")
            };

            var dystopiaBooks = books.Where(b => b.Genre == "Dystopia").ToList();

            Assert.Equal(2, dystopiaBooks.Count);
            Assert.All(dystopiaBooks, b => Assert.Equal("Dystopia", b.Genre));
        }

        [Theory]
        [MemberData(nameof(TestData.PermissionsTestData), MemberType = typeof(TestData))]
        public void Permissions_CanManageCatalog_Test(string role, bool expectedResult)
        {
            bool hasAccess = role == "Admin" || role == "Librarian";

            Assert.Equal(expectedResult, hasAccess);
        }

        [Fact]
        public void Author_NameFormatting_ReturnsCorrectInitials()
        {
            string firstName = "Taras";
            string lastName = "Shevchenko";

            string formatted = $"{lastName} {firstName[0]}.";

            Assert.Equal("Shevchenko T.", formatted);
        }

        [Theory]
        [MemberData(nameof(TestData.PageCountTestData), MemberType = typeof(TestData))]
        public void Book_PageCountValidation_Test(int pages, bool expectedIsValid)
        {
            bool isValid = pages > 0;

            Assert.Equal(expectedIsValid, isValid);
        }

        [Fact]
        public void Loan_ExtendDueDate_AddsStandardPeriod()
        {
            DateTime initialDueDate = new DateTime(2026, 11, 10);
            int extensionDays = 14;

            DateTime newDueDate = initialDueDate.AddDays(extensionDays);

            Assert.Equal(new DateTime(2026, 11, 24), newDueDate);
        }

        [Fact]
        public void Catalog_SortByYear_AscendingOrder()
        {
            var books = new List<(string Title, short Year)>
            {
                ("Book C", 2020),
                ("Book A", 1995),
                ("Book B", 2015)
            };

            var sortedBooks = books.OrderBy(b => b.Year).ToList();

            Assert.Equal("Book A", sortedBooks[0].Title);
            Assert.Equal("Book B", sortedBooks[1].Title);
            Assert.Equal("Book C", sortedBooks[2].Title);
        }

        [Fact]
        public void Reader_DebtStatus_HasUnpaidFines_ReturnsTrue()
        {
            decimal totalFines = 45.50m;
            bool hasDebt = totalFines > 0m;

            Assert.True(hasDebt, "Читач має неоплачені штрафи.");
        }
    }
}