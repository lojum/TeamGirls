using BusinessLogic.Services;

namespace SmartLib.Tests.Services
{
    public class PasswordHasherTests
    {
        [Fact]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            var hash = PasswordHasher.Hash("Secret123");

            Assert.True(PasswordHasher.Verify("Secret123", hash));
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            var hash = PasswordHasher.Hash("Secret123");

            Assert.False(PasswordHasher.Verify("secret123", hash));
        }

        [Fact]
        public void Hash_SamePasswordTwice_ProducesDifferentHashes()
        {
            // Сіль випадкова, тому однакові паролі мають різні хеші
            Assert.NotEqual(PasswordHasher.Hash("Secret123"), PasswordHasher.Hash("Secret123"));
        }

        [Fact]
        public void Hash_HasExpectedFormat()
        {
            var parts = PasswordHasher.Hash("Secret123").Split('$');

            Assert.Equal(4, parts.Length);
            Assert.Equal("pbkdf2-sha256", parts[0]);
            Assert.Equal("210000", parts[1]);
        }

        [Fact]
        public void Verify_SeedHashFromSchema_RejectsWrongPassword()
        {
            // Хеш тестових користувачів із schema.sql розбирається без винятків
            const string seedHash = "pbkdf2-sha256$210000$eSmTTHQLnU28EhmlAy+2Ww==$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=";

            Assert.False(PasswordHasher.Verify("definitely-wrong", seedHash));
        }

        [Fact]
        public void Verify_BcryptHash_IsSupported()
        {
            var bcryptHash = BCrypt.Net.BCrypt.HashPassword("Legacy123");

            Assert.True(PasswordHasher.Verify("Legacy123", bcryptHash));
            Assert.False(PasswordHasher.Verify("Other123", bcryptHash));
        }

        // Крайові випадки: зіпсований хеш повертає false, а не кидає виняток
        [Theory]
        [InlineData("")]
        [InlineData("not-a-hash")]
        [InlineData("$2a$broken")]
        [InlineData("md5$210000$AAAA$BBBB")]
        [InlineData("pbkdf2-sha256$abc$AAAA$BBBB")]
        [InlineData("pbkdf2-sha256$0$eSmTTHQLnU28EhmlAy+2Ww==$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=")]
        [InlineData("pbkdf2-sha256$9999999$eSmTTHQLnU28EhmlAy+2Ww==$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=")]
        [InlineData("pbkdf2-sha256$210000$not base64!$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=")]
        [InlineData("pbkdf2-sha256$210000$AAAA$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=")]
        public void Verify_MalformedHash_ReturnsFalse(string badHash)
        {
            Assert.False(PasswordHasher.Verify("any", badHash));
        }
    }
}
