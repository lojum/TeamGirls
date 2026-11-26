namespace SmartLib.IntegrationTests.TestData
{
    public static class SecurityTestData
    { 
        public static TheoryData<string> SqlInjectionAttempts => new()
        {
            "'; DROP TABLE books; --",
            "' OR '1'='1",
            "%' OR 1=1--",
            "\\'; DELETE FROM users; --"
        };

        public const string Pbkdf2Prefix = "pbkdf2-sha256$";
        public const string BcryptPrefix = "$2";
    }
}
