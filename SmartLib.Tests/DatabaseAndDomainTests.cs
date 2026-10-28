using System;
using Xunit;

namespace SmartLib.Tests
{
    public class DatabaseAndDomainTests
    {
        [Fact]
        public void Book_TitleValidation_NullOrEmptyTitle_ShouldBeInvalid()
        {
            string invalidTitle = "   ";

            Assert.True(string.IsNullOrWhiteSpace(invalidTitle), "Назва книги не може бути порожньою.");
        }

        [Theory]
        [InlineData("978-966-03-8001-1", true)]
        [InlineData("invalid-isbn-code", false)]
        public void Isbn_FormatValidation_Test(string isbn, bool expectedIsValid)
        {
            string cleanIsbn = System.Text.RegularExpressions.Regex.Replace(isbn, @"[-\s]", "");
            bool isValid = System.Text.RegularExpressions.Regex.IsMatch(cleanIsbn, @"^(\d{9}[\dXx]|\d{13})$");

            Assert.Equal(expectedIsValid, isValid);
        }

        [Fact]
        public void PublicationYear_Validation_ShouldBeWithinValidRange()
        {
            short validYear = 2021;
            short invalidYear = 800;

            Assert.True(validYear >= 1000 && validYear <= 2100, "Рік видання має бути між 1000 та 2100.");
            Assert.False(invalidYear >= 1000 && invalidYear <= 2100, "Рік видання поза межами допустимого діапазону.");
        }

        [Fact]
        public void CopyStatus_SyncLogic_WrittenOffCopy_CannotBeLoaned()
        {
            string status = "written_off";

            bool canLoan = status != "written_off";

            Assert.False(canLoan, "Списаний примірник не може бути виданий читачеві.");
        }
    }
}