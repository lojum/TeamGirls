using System.Windows;
using BusinessLogic.Services;
using Domain.Entities;
using Domain.Enums;
using Presentation.ViewModels;
using SmartLib.Tests.Helpers;
using SmartLib.Tests.TestData;
using SmartLib.Tests;
using Xunit;

namespace SmartLib.Tests.ViewModels
{
    public class LoginViewModelTests
    {
        [Theory]
        [MemberData(nameof(RegistrationTestData.InvalidEmails), MemberType = typeof(RegistrationTestData))]
        public async Task LoginAsync_InvalidEmail_ShowsErrorWithoutAuthentication(string email)
        {
            var auth = new FakeAuthService();
            var navigation = new FakeNavigationService();
            var viewModel = new LoginViewModel(auth, navigation)
            {
                Email = email,
                Password = "StrongPass123!"
            };

            await viewModel.LoginAsync();

            Assert.Equal(Visibility.Visible, viewModel.EmailErrorVisibility);
            Assert.Equal(0, auth.AuthenticateCallCount);
        }

        [Fact]
        public async Task LoginAsync_NullUser_ShowsIncorrectCredentials()
        {
            var auth = new FakeAuthService { UserToReturn = null };
            var viewModel = new LoginViewModel(auth, new FakeNavigationService())
            {
                Email = "reader@smartlib.ua",
                Password = "wrong-password"
            };

            await viewModel.LoginAsync();

            Assert.Equal("Incorrect email or password, or the account is inactive.", viewModel.LoginErrorMessage);
            Assert.Equal(Visibility.Visible, viewModel.LoginErrorVisibility);
            Assert.True(viewModel.IsSubmitEnabled);
        }

        [Fact]
        public async Task LoginAsync_ServiceThrows_ShowsDatabaseMessageAndReenablesSubmit()
        {
            var auth = new FakeAuthService { ExceptionToThrow = new InvalidOperationException("Database unavailable") };
            var viewModel = new LoginViewModel(auth, new FakeNavigationService())
            {
                Email = "reader@smartlib.ua",
                Password = "StrongPass123!"
            };

            await viewModel.LoginAsync();

            Assert.Equal("Could not connect to the database. Try again later.", viewModel.LoginErrorMessage);
            Assert.Equal(Visibility.Visible, viewModel.LoginErrorVisibility);
            Assert.True(viewModel.IsSubmitEnabled);
        }

        [Theory]
        [MemberData(nameof(LoginTestData.Roles), MemberType = typeof(LoginTestData))]
        public async Task LoginAsync_RoleOpensMatchingDashboard(UserRole role)
        {
            var auth = new FakeAuthService
            {
                UserToReturn = new User
                {
                    UserId = 42,
                    FullName = "Test User",
                    Role = role
                }
            };
            var navigation = new FakeNavigationService();
            var viewModel = new LoginViewModel(auth, navigation)
            {
                Email = "test@smartlib.ua",
                Password = "StrongPass123!"
            };

            await viewModel.LoginAsync();

            if (role == UserRole.Librarian)
                Assert.Equal(("Test User", 42), navigation.LastLibrarian);
            else if (role == UserRole.Reader)
                Assert.Equal(("Test User", 42), navigation.LastReader);
            else
                Assert.Equal("Test User", navigation.LastAdminName);
        }
    }
}
