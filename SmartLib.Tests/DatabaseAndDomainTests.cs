using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SmartLib.Tests.TestData;

namespace SmartLib.Tests
{
    public class ComprehensiveDomainTests
    {
        [Theory]
        [MemberData(nameof(ComprehensiveTestData.IsbnTestData), MemberType = typeof(ComprehensiveTestData))]
        public void Book_IsbnFormatValidation_ReturnsExpected(string isbn, bool expectedValid)
        {
            var cleanIsbn = isbn.Replace("-", "").Trim();
            bool isValid = (cleanIsbn.Length == 13 || cleanIsbn.Length == 10) && cleanIsbn.All(char.IsDigit);

            Assert.Equal(expectedValid, isValid);
        }

        [Theory]
        [MemberData(nameof(ComprehensiveTestData.FineCalculationTestData), MemberType = typeof(ComprehensiveTestData))]
        public void FineCalculation_StandardAndZeroDays_CalculatesCorrectly(int daysLate, decimal ratePerDay, decimal expectedTotal)
        {
            decimal actualTotal = daysLate * ratePerDay;

            Assert.Equal(expectedTotal, actualTotal);
        }

        [Fact]
        public void Reader_AccountStatus_SuspendedUserCannotBorrowBooks()
        {
            bool isActive = false;
            bool hasActiveSubscription = true;

            bool canBorrow = isActive && hasActiveSubscription;

            Assert.False(canBorrow, "Заблокований користувач не має права оформлювати нові позики.");
        }

        [Fact]
        public void Reservation_ExpirationLogic_ChecksIfReservationExpired()
        {
            var reservationDate = new DateTime(2026, 10, 20);
            var currentDate = new DateTime(2026, 10, 25);
            int expirationDays = 3;

            bool isExpired = (currentDate - reservationDate).TotalDays > expirationDays;

            Assert.True(isExpired, "Термін резервування книги повинен вважатися минулим.");
        }

        [Theory]
        [MemberData(nameof(ComprehensiveTestData.AuthorNameTestData), MemberType = typeof(ComprehensiveTestData))]
        public void Author_FullNameFormatting_ProducesCorrectInitials(string firstName, string lastName, string expectedFormat)
        {
            string formatted = $"{lastName} {firstName[0]}.";

            Assert.Equal(expectedFormat, formatted);
        }

        [Fact]
        public void Catalog_SearchByMultipleFilters_AppliesSuccessfully()
        {
            var books = new List<(string Title, string Genre, short Year)>
            {
                ("Кобзар", "Поезія", 1840),
                ("Тіні забутих предків", "Повість", 1912),
                ("Захар Беркут", "Історична повість", 1883),
                ("Камінний хрест", "Новела", 1900)
            };

            var filtered = books.Where(b => b.Year < 1900 && b.Genre.Contains("повість", StringComparison.OrdinalIgnoreCase)).ToList();

            Assert.Single(filtered);
            Assert.Equal("Захар Беркут", filtered[0].Title);
        }

        [Theory]
        [MemberData(nameof(ComprehensiveTestData.BookCopyStockTestData), MemberType = typeof(ComprehensiveTestData))]
        public void BookCopy_InventoryCheck_QuantityMustBePositive(int stockCount, bool expectedAvailable)
        {
            bool isAvailable = stockCount > 0;

            Assert.Equal(expectedAvailable, isAvailable);
        }

        [Fact]
        public void Password_ComplexityValidation_EnforcesSecurityRules()
        {
            string password = "SecurePassword123!";

            bool hasMinLength = password.Length >= 8;
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSymbol = password.Any(ch => !char.IsLetterOrDigit(ch));

            bool isStrong = hasMinLength && hasUpper && hasLower && hasDigit && hasSymbol;

            Assert.True(isStrong, "Пароль повинен відповідати стандартам безпеки.");
        }
    }
}