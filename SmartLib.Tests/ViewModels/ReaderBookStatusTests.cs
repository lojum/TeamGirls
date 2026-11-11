using Presentation.ViewModels;
using SmartLib.Tests.TestData;

namespace SmartLib.Tests.ViewModels
{
    [Trait("Category", "Unit")]
    public class ReaderBookStatusTests
    {
        [Fact]
        public void Describe_BorrowedByReader_ShowsDueDate()
        {
            // Act
            var (text, isMuted) = ReaderBookStatus.Describe(availableCopies: 2, LibrarianTestData.DueDate);

            // Assert
            Assert.Equal("Borrowed · due 2026-11-03", text);
            Assert.True(isMuted);
        }

        [Fact]
        public void Describe_FreeCopies_ShowsAvailableCount()
        {
            var (text, isMuted) = ReaderBookStatus.Describe(availableCopies: 2, myDueDate: null);

            Assert.Equal("Available (2)", text);
            Assert.False(isMuted);
        }

        [Fact]
        public void Describe_NoFreeCopies_ShowsNotAvailable()
        {
            var (text, isMuted) = ReaderBookStatus.Describe(availableCopies: 0, myDueDate: null);

            Assert.Equal("Not available", text);
            Assert.True(isMuted);
        }
    }
}
