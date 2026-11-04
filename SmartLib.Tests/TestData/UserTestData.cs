namespace SmartLib.Tests.TestData
{
    // Тестові користувачі, спільні для AuthServiceTests і LoanServiceTests
    public static class UserTestData
    {
        public const string ReaderEmail = "olena.b@gmail.com";
        public const string ReaderName = "Олена Бондаренко";
        public const string ReaderPassword = "Reader123!";
        public const string WrongPassword = "wrong-password";
        public const string UnknownEmail = "nobody@gmail.com";
        public const string LibrarianEmail = "librarian@smartlib.ua";

        // Email читача в іншому регістрі та з пробілами: має знаходити того самого користувача
        public static TheoryData<string> ReaderEmailVariants => new()
        {
            "Olena.B@Gmail.com",
            "  olena.b@gmail.com  ",
            "OLENA.B@GMAIL.COM"
        };

        // Порожній email або пароль
        public static TheoryData<string, string> EmptyCredentials => new()
        {
            { "", ReaderPassword },
            { "   ", ReaderPassword },
            { ReaderEmail, "" }
        };
    }
}
