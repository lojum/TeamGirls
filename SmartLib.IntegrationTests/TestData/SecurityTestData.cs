namespace SmartLib.IntegrationTests.TestData
{
    // Дані для SecurityIntegrationTests
    public static class SecurityTestData
    {
        // Рядки, які намагаються змінити SQL-запит; EF Core передає їх як параметри, тож вони лишаються звичайним текстом
        public static TheoryData<string> SqlInjectionAttempts => new()
        {
            "'; DROP TABLE books; --",
            "' OR '1'='1",
            "%' OR 1=1--",
            "\\'; DELETE FROM users; --"
        };

        // Префікси безпечних хешів: PBKDF2 (PasswordHasher) і BCrypt (старі записи)
        public const string Pbkdf2Prefix = "pbkdf2-sha256$";
        public const string BcryptPrefix = "$2";
    }
}
