using BusinessLogic.Services;
using SmartLib.Tests.TestData;

namespace SmartLib.Tests.Services
{
    [Trait("Category", "Unit")]
    public class PasswordHasherTests
    {
        [Fact]
        public void Verify_CorrectPassword_ReturnsTrue()
        {
            // Arrange
            var hash = PasswordHasher.Hash(PasswordTestData.Password);

            // Act
            var isValid = PasswordHasher.Verify(PasswordTestData.Password, hash);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            // Arrange
            var hash = PasswordHasher.Hash(PasswordTestData.Password);

            // Act
            var isValid = PasswordHasher.Verify(PasswordTestData.Password.ToLower(), hash);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void Hash_SamePasswordTwice_ProducesDifferentHashes()
        {
            // Act
            var first = PasswordHasher.Hash(PasswordTestData.Password);
            var second = PasswordHasher.Hash(PasswordTestData.Password);

            // Assert: сіль випадкова, тому однакові паролі мають різні хеші
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Hash_HasExpectedFormat()
        {
            // Act
            var parts = PasswordHasher.Hash(PasswordTestData.Password).Split('$');

            // Assert
            Assert.Equal(4, parts.Length);
            Assert.Equal("pbkdf2-sha256", parts[0]);
            Assert.Equal("210000", parts[1]);
        }

        [Fact]
        public void Verify_SeedHashFromSchema_RejectsWrongPassword()
        {
            // Act: хеш тестових користувачів із schema.sql розбирається без винятків
            var isValid = PasswordHasher.Verify(UserTestData.WrongPassword, PasswordTestData.SeedHash);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void Verify_BcryptHash_IsSupported()
        {
            // Arrange
            var bcryptHash = BCrypt.Net.BCrypt.HashPassword(PasswordTestData.LegacyPassword);

            // Act & Assert
            Assert.True(PasswordHasher.Verify(PasswordTestData.LegacyPassword, bcryptHash));
            Assert.False(PasswordHasher.Verify(PasswordTestData.Password, bcryptHash));
        }

        [Theory]
        [MemberData(nameof(PasswordTestData.MalformedHashes), MemberType = typeof(PasswordTestData))]
        public void Verify_MalformedHash_ReturnsFalse(string badHash)
        {
            // Act
            var isValid = PasswordHasher.Verify(PasswordTestData.Password, badHash);

            // Assert
            Assert.False(isValid);
        }
    }
}
