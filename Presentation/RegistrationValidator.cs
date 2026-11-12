using System.Net.Mail;

namespace Presentation.Validators
{
    // Правила форми реєстрації в одному місці: їх використовують і кнопка, і повідомлення під полями
    public static class RegistrationValidator
    {
        public const int MaxNameLength = 150;
        public const int MinPasswordLength = 8;

        public static bool IsValidName(string fullName) =>
            !string.IsNullOrWhiteSpace(fullName) && fullName.Length <= MaxNameLength;

        public static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) && MailAddress.TryCreate(email, out _);

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
