using BusinessLogic.Services;
using Presentation.ViewModels;
using SmartLib.Tests.Helpers;
using SmartLib.Tests.TestData;
using static SmartLib.Tests.TestData.LibrarianTestData;

namespace SmartLib.Tests.ViewModels
{
    [Trait("Category", "Unit")]
    public class LibrarianDashboardViewModelTests
    {
        private static (LibrarianDashboardViewModel ViewModel, FakeLoanService Loans, FakeLibrarianDashboardService Dashboard) Arrange()
        {
            var loans = new FakeLoanService();
            var dashboard = new FakeLibrarianDashboardService { Copies = Copies(), Readers = Readers() };
            var viewModel = new LibrarianDashboardViewModel(
                dashboard, loans, new FakeNavigationService(), new FixedTimeProvider(LoanTestData.Now));
            viewModel.SetLibrarian(LibrarianName, LibrarianId);
            return (viewModel, loans, dashboard);
        }

        [Fact]
        public async Task Load_FillsTableReadersAndAvailableCopies()
        {
            // Arrange
            var (viewModel, _, _) = Arrange();

            // Act
            await viewModel.LoadAsync();

            // Assert
            Assert.Equal(Copies().Count, viewModel.Books.Count);
            Assert.Equal(Readers().Count, viewModel.Readers.Count);
            Assert.Equal(AvailableInventory, Assert.Single(viewModel.AvailableBooks).BookId);
            Assert.False(viewModel.IsError);
        }

        [Fact]
        public async Task Load_DatabaseError_ShowsErrorMessage()
        {
            // Arrange
            var (viewModel, _, dashboard) = Arrange();
            dashboard.ThrowOnLoad = true;

            // Act
            await viewModel.LoadAsync();

            // Assert
            Assert.True(viewModel.IsError);
            Assert.Contains("Could not load data", viewModel.StatusMessage);
        }

        [Fact]
        public async Task SearchText_FiltersTable()
        {
            // Arrange
            var (viewModel, _, _) = Arrange();
            await viewModel.LoadAsync();

            // Act
            viewModel.SearchText = "орвелл";

            // Assert
            Assert.Equal(LoanedInventory, Assert.Single(viewModel.Books).BookId);
        }

        [Fact]
        public async Task IssueBook_Success_CallsLoanServiceAndShowsMessage()
        {
            // Arrange
            var (viewModel, loans, dashboard) = Arrange();
            await viewModel.LoadAsync();
            viewModel.SelectedReader = viewModel.Readers[0];
            viewModel.SelectedBook = viewModel.AvailableBooks[0];
            viewModel.SelectedDuration = viewModel.LoanDurations.Single(d => d.Days == 21);

            // Act
            await viewModel.IssueBookAsync();

            // Assert: передано інвентарний номер, email читача, id бібліотекаря і термін
            Assert.Equal((AvailableInventory, UserTestData.ReaderEmail, LibrarianId, 21), loans.LastIssue);
            Assert.False(viewModel.IsError);
            Assert.Contains("issued to", viewModel.StatusMessage);
            Assert.Null(viewModel.SelectedBook);
            Assert.Equal(2, dashboard.LoadCount); // після видачі дані перезавантажено
        }

        [Fact]
        public async Task IssueBook_LoanException_ShowsServiceMessage()
        {
            // Arrange
            var (viewModel, loans, _) = Arrange();
            await viewModel.LoadAsync();
            viewModel.SelectedReader = viewModel.Readers[0];
            viewModel.SelectedBook = viewModel.AvailableBooks[0];
            loans.ExceptionToThrow = new LoanException("Reader account is inactive.");

            // Act
            await viewModel.IssueBookAsync();

            // Assert
            Assert.True(viewModel.IsError);
            Assert.Equal("Reader account is inactive.", viewModel.StatusMessage);
        }

        [Fact]
        public async Task IssueBook_NothingSelected_DoesNotCallLoanService()
        {
            // Arrange
            var (viewModel, loans, _) = Arrange();
            await viewModel.LoadAsync();

            // Act
            await viewModel.IssueBookAsync();

            // Assert
            Assert.Null(loans.LastIssue);
            Assert.True(viewModel.IsError);
        }

        [Fact]
        public void CalculatedDueDate_UsesSelectedDuration()
        {
            // Arrange
            var (viewModel, _, _) = Arrange();

            // Act
            viewModel.SelectedDuration = viewModel.LoanDurations.Single(d => d.Days == 7);

            // Assert
            var expected = LoanTestData.Now.ToLocalTime().Date.AddDays(7).ToString("yyyy-MM-dd");
            Assert.Equal(expected, viewModel.CalculatedDueDate);
        }
    }
}
