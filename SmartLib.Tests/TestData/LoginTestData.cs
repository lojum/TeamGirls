using Domain.Enums;
using Xunit;

namespace SmartLib.Tests.TestData
{
    public static class LoginTestData
    {
        public const string ReaderEmail = "reader@smartlib.ua";
        public const string TestEmail = "test@smartlib.ua";
        public const string ValidPassword = "StrongPass123!";
        public const string WrongPassword = "wrong-password";
        public const string TestUserName = "Test User";
        public const string IncorrectCredentialsMessage = "Incorrect email or password, or the account is inactive.";
        public const string DatabaseErrorMessage = "Could not connect to the database. Try again later.";
        public const string DatabaseExceptionMessage = "Database unavailable";
        public const long TestUserId = 42;

        public static TheoryData<UserRole> Roles => new()
        {
            UserRole.Librarian,
            UserRole.Reader,
            UserRole.Admin
        };
    }
}
