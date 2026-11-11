namespace SmartLib.IntegrationTests.TestData
{
    public static class ConstraintTestData
    {
        public const string BookTitle = "Тестова книга";
        public const short ValidYear = 2020;
        public const string PasswordHash = "test-hash";
        public const string UserName = "Тест Читач";

        public const string NewReaderEmail = "New.Reader@Gmail.com";
        public const string NewReaderPassword = "Secret123!";

        public static TheoryData<string> ValidIsbns => new()
        {
            "0-306-40615-2",
            "080442957X",
            "978-0-306-40615-7",
            "978 966 03 8001 1"
        };

        public static TheoryData<string> InvalidIsbns => new()
        {
            "12345",             
            "978-966-03-ABCD",      
            "97896603800111",       
            "X804429570"            
        };

        public static TheoryData<short> BoundaryYears => new()
        {
            1000,
            2100
        };

        public static TheoryData<short> InvalidYears => new()
        {
            999,
            2101
        };

        public static TheoryData<string> InvalidEmails => new()
        {
            "not-an-email",
            "reader@domain",
            "@gmail.com"
        };

        public static TheoryData<string> ExistingEmailVariants => new()
        {
            "OLENA.B@GMAIL.COM",
            "Olena.B@gmail.com"
        };
    }
}
