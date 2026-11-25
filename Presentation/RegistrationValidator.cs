using System.Text.RegularExpressions;

namespace Presentation.Validators
{
    // Правила форми реєстрації в одному місці: їх використовують і кнопка, і повідомлення під полями
    public static class RegistrationValidator
    {
        private static readonly Regex EmailPattern = new(
            @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
            RegexOptions.CultureInvariant);

        public const int MaxNameLength = 150;
        public const int MinPasswordLength = 8;
        public const int MaxEmailLength = 255;

        public static bool IsValidName(string fullName) =>
            !string.IsNullOrWhiteSpace(fullName) && fullName.Length <= MaxNameLength;

        public static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email)
            && email.Trim().Length <= MaxEmailLength
            && EmailPattern.IsMatch(email.Trim());

        public static bool IsValidPassword(string password) =>
            !string.IsNullOrWhiteSpace(password) && password.Length >= MinPasswordLength;

        public static bool PasswordsMatch(string password, string confirmPassword) =>
            password == confirmPassword;

        public static bool IsValidInput(string fullName, string email, string password, string confirmPassword) =>
            IsValidName(fullName)
            && IsValidEmail(email)
            && IsValidPassword(password)
            && PasswordsMatch(password, confirmPassword);
    }
}
