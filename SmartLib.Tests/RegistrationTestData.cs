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
}
