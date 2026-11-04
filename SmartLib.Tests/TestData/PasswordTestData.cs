namespace SmartLib.Tests.TestData
{
    public static class PasswordTestData
    {
        public const string Password = "Secret123";
        public const string LegacyPassword = "Legacy123";

        // Сіль і хеш тестових користувачів із schema.sql
        public const string SeedSalt = "eSmTTHQLnU28EhmlAy+2Ww==";
        public const string SeedDigest = "PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=";
        public const string SeedHash = $"pbkdf2-sha256$210000${SeedSalt}${SeedDigest}";

        // Крайові випадки: зіпсований хеш має повертати false, а не кидати виняток
        public static TheoryData<string> MalformedHashes => new()
        {
            "",                                                    // порожній рядок
            "not-a-hash",                                          // немає роздільників
            "$2a$broken",                                          // зіпсований BCrypt
            "md5$210000$AAAA$BBBB",                                // невідомий алгоритм
            "pbkdf2-sha256$abc$AAAA$BBBB",                         // ітерації не число
            $"pbkdf2-sha256$0${SeedSalt}${SeedDigest}",            // 0 ітерацій
            $"pbkdf2-sha256$9999999${SeedSalt}${SeedDigest}",      // забагато ітерацій
            $"pbkdf2-sha256$210000$not base64!${SeedDigest}",      // невалідний base64
            $"pbkdf2-sha256$210000$AAAA${SeedDigest}"              // неправильна довжина солі
        };
    }
}
