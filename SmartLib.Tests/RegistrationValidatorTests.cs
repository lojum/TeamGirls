using Presentation.Validators;

namespace SmartLib.Tests
{
    // Окремі правила форми реєстрації; разом їх перевіряє RegistrationViewModelTests
    [Trait("Category", "Unit")]
    public class RegistrationValidatorTests
    {
        [Theory]
        [MemberData(nameof(RegistrationTestData.NameCases), MemberType = typeof(RegistrationTestData))]
        public void IsValidName_ChecksLengthAndBlank(string name, bool expected)
        {
            Assert.Equal(expected, RegistrationValidator.IsValidName(name));
        }

        [Theory]
        [MemberData(nameof(RegistrationTestData.PasswordCases), MemberType = typeof(RegistrationTestData))]
        public void IsValidPassword_ChecksMinimumLength(string password, bool expected)
        {
            Assert.Equal(expected, RegistrationValidator.IsValidPassword(password));
        }

        [Fact]
        public void PasswordsMatch_IsCaseSensitive()
        {
            Assert.True(RegistrationValidator.PasswordsMatch(RegistrationTestData.ValidPassword, RegistrationTestData.ValidPassword));
            Assert.False(RegistrationValidator.PasswordsMatch(RegistrationTestData.ValidPassword, RegistrationTestData.ValidPassword.ToLower()));
        }

        [Fact]
        public void IsValidEmail_RejectsMalformedAddress()
        {
            Assert.True(RegistrationValidator.IsValidEmail(RegistrationTestData.ValidEmail));
            Assert.False(RegistrationValidator.IsValidEmail("bad-email"));
        }
    }
}
