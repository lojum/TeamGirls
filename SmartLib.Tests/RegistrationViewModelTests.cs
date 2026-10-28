using Presentation.ViewModels;
using Presentation.Validators;
using Xunit;

namespace SmartLib.Tests
{
    public class RegistrationViewModelTests
    {
        [Fact]
        public void RegistrationValidator_ValidData_ReturnsTrue()
        {
            bool isValid = RegistrationValidator.IsValidInput("Оксана Мандзювата", "oksana@smartlib.ua", "StrongPass123!", "StrongPass123!");
            Assert.True(isValid);
        }

        // Перевірка крайової ситуації: порожні рядки або пробіли
        [Fact]
        public void RegistrationValidator_OnlySpaces_ReturnsFalse()
        {
            bool isValid = RegistrationValidator.IsValidInput("   ", "   ", "   ", "   ");
            Assert.False(isValid);
        }

        // Перевірка крайових ситуацій: некоректні дані
        [Theory]
        [InlineData("Оксана", "bad-email", "Pass123!", "Pass123!")] // Некоректний email
        [InlineData("Оксана", "oksana@smartlib.ua", "123", "123")] // Занадто короткий пароль
        [InlineData("Оксана", "oksana@smartlib.ua", "Pass123!", "Different456!")] // Паролі не збігаються
        public void RegistrationValidator_InvalidData_ReturnsFalse(string name, string email, string pass, string confirm)
        {
            bool isValid = RegistrationValidator.IsValidInput(name, email, pass, confirm);
            Assert.False(isValid);
        }

        [Fact]
        public void RegistrationViewModel_IsSubmitEnabled_UpdatesCorrectly()
        {
            // Передаємо null замість Window, оскільки тестуємо лише логіку ViewModel
            var viewModel = new RegistrationViewModel(null);

            viewModel.FullName = "Оксана Мандзювата";
            viewModel.Email = "oksana@smartlib.ua";
            viewModel.Password = "StrongPass123!";
            viewModel.ConfirmPassword = "StrongPass123!";

            Assert.True(viewModel.IsSubmitEnabled);
        }
    }
}