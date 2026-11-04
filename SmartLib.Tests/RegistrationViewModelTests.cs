using BusinessLogic.Services;
using Presentation.ViewModels;
using Presentation.Services;
using Presentation.Validators;
using Xunit;

namespace SmartLib.Tests
{
    public class RegistrationViewModelTests
    {
        [Theory]
        [MemberData(nameof(RegistrationTestData.ValidInputs), MemberType = typeof(RegistrationTestData))]
        public void RegistrationValidator_ValidData_ReturnsTrue(
            string fullName,
            string email,
            string password,
            string confirmPassword)
        {
            bool isValid = RegistrationValidator.IsValidInput(fullName, email, password, confirmPassword);
            Assert.True(isValid);
        }

        [Theory]
        [MemberData(nameof(RegistrationTestData.InvalidInputs), MemberType = typeof(RegistrationTestData))]
        public void RegistrationValidator_InvalidData_ReturnsFalse(string name, string email, string pass, string confirm)
        {
            bool isValid = RegistrationValidator.IsValidInput(name, email, pass, confirm);
            Assert.False(isValid);
        }

        [Fact]
        public void RegistrationViewModel_IsSubmitEnabled_UpdatesCorrectly()
        {
            var viewModel = new RegistrationViewModel(new TestUserService(), new TestNavigationService());

            viewModel.FullName = RegistrationTestData.ValidFullName;
            viewModel.Email = RegistrationTestData.ValidEmail;
            viewModel.Password = RegistrationTestData.ValidPassword;
            viewModel.ConfirmPassword = RegistrationTestData.ValidPassword;

            Assert.True(viewModel.IsSubmitEnabled);
        }

        private sealed class TestUserService : IUserService
        {
            public Task<AuthenticatedUser?> AuthenticateAsync(string email, string password) =>
                Task.FromResult<AuthenticatedUser?>(null);

            public Task RegisterReaderAsync(string fullName, string email, string password) =>
                Task.CompletedTask;
        }

        private sealed class TestNavigationService : IWindowNavigationService
        {
            public void ShowMain() { }
            public void ShowLogin() { }
            public void ShowRegistration() { }
            public void ShowAdminDashboard(string adminName) { }
            public void ShowLibrarianDashboard(string librarianName) { }
            public void ShowReaderDashboard(string readerName) { }
        }
    }
}