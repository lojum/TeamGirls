using Xunit;

namespace SmartLib.Tests;

public static class RegistrationTestData
{
    public const string ValidFullName = "Оксана Мандзювата";
    public const string ValidEmail = "oksana@smartlib.ua";
    public const string ValidPassword = "StrongPass123!";

    public static TheoryData<string, string, string, string> ValidInputs => new()
    {
        { ValidFullName, ValidEmail, ValidPassword, ValidPassword }
    };

    public static TheoryData<string, string, string, string> InvalidInputs => new()
    {
        { "   ", "   ", "   ", "   " },
        { "Оксана", "bad-email", "Pass123!", "Pass123!" },
        { "Оксана", "oksana@smartlib.ua", "123", "123" },
        { "Оксана", "oksana@smartlib.ua", "Pass123!", "Different456!" }
    };

    // Межі правил RegistrationValidator: довжина імені (до 150) і пароля (від 8)
    public static TheoryData<string, bool> NameCases => new()
    {
        { new string('А', 150), true },
        { new string('А', 151), false },
        { "   ", false }
    };

    public static TheoryData<string, bool> PasswordCases => new()
    {
        { "Pass123", false },
        { "Pass1234", true },
        { "        ", false }
    };
}
