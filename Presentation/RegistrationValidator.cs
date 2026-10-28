using System.Net.Mail;

namespace Presentation.Validators
{
    public static class RegistrationValidator
    {
        public static bool IsValidInput(string fullName, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
                return false;

            if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out _))
                return false;

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            if (password != confirmPassword)
                return false;

            return true;
        }
    }
}