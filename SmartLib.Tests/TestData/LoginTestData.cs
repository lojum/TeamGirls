using Domain.Enums;
using Xunit;

namespace SmartLib.Tests.TestData
{
    public static class LoginTestData
    {
        public static TheoryData<UserRole> Roles => new()
        {
            UserRole.Librarian,
            UserRole.Reader,
            UserRole.Admin
        };
    }
}
