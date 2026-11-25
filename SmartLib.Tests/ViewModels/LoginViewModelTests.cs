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
    [Trait("Category", "Unit")]
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
                Password = LoginTestData.ValidPassword
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
                Email = LoginTestData.ReaderEmail,
                Password = LoginTestData.WrongPassword
            };

            await viewModel.LoginAsync();

            Assert.Equal(LoginTestData.IncorrectCredentialsMessage, viewModel.LoginErrorMessage);
            Assert.Equal(Visibility.Visible, viewModel.LoginErrorVisibility);
            Assert.True(viewModel.IsSubmitEnabled);
        }

        [Fact]
        public async Task LoginAsync_ServiceThrows_ShowsDatabaseMessageAndReenablesSubmit()
        {
            var auth = new FakeAuthService
            {
                ExceptionToThrow = new InvalidOperationException(LoginTestData.DatabaseExceptionMessage)
            };
            var viewModel = new LoginViewModel(auth, new FakeNavigationService())
            {
                Email = LoginTestData.ReaderEmail,
                Password = LoginTestData.ValidPassword
            };

            await viewModel.LoginAsync();

            Assert.Equal(LoginTestData.DatabaseErrorMessage, viewModel.LoginErrorMessage);
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
                    UserId = LoginTestData.TestUserId,
                    FullName = LoginTestData.TestUserName,
                    Role = role
                }
            };
            var navigation = new FakeNavigationService();
            var viewModel = new LoginViewModel(auth, navigation)
            {
                Email = LoginTestData.TestEmail,
                Password = LoginTestData.ValidPassword
            };

            await viewModel.LoginAsync();

            if (role == UserRole.Librarian)
                Assert.Equal((LoginTestData.TestUserName, LoginTestData.TestUserId), navigation.LastLibrarian);
            else if (role == UserRole.Reader)
                Assert.Equal((LoginTestData.TestUserName, LoginTestData.TestUserId), navigation.LastReader);
            else
                Assert.Equal(LoginTestData.TestUserName, navigation.LastAdminName);
        }
    }
}
