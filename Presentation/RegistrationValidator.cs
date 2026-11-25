using System.Text.RegularExpressions;

namespace Presentation.Validators
{
    public static class RegistrationValidator
    {
        private static readonly Regex EmailPattern = new(
            @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
            RegexOptions.CultureInvariant);

        public const int MaxEmailLength = 255;

        public static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email)
            && email.Trim().Length <= MaxEmailLength
            && EmailPattern.IsMatch(email.Trim());

        public static bool IsValidInput(string fullName, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
                return false;

            if (!IsValidEmail(email))
                return false;

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            if (password != confirmPassword)
                return false;

            return true;
        }
    }
}