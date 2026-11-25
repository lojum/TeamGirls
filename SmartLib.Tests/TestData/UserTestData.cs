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
        public const string LibrarianName = "Оксана Мандзювата";

        // Email читача в іншому регістрі та з пробілами: має знаходити того самого користувача
        public static TheoryData<string> ReaderEmailVariants => new()
        {
            "Olena.B@Gmail.com",
            "  olena.b@gmail.com  ",
            "OLENA.B@GMAIL.COM"
        };

        // Невдалий вхід: (email, пароль, чи активний читач, причина в журналі)
        public static TheoryData<string, string, bool, string> FailedSignIns => new()
        {
            { UnknownEmail, ReaderPassword, true, "account not found" },
            { ReaderEmail, ReaderPassword, false, "account is inactive" },
            { ReaderEmail, WrongPassword, true, "wrong password" }
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
