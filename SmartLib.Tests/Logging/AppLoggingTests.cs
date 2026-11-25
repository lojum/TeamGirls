using System.IO;
using BusinessLogic.Services;
using Microsoft.Extensions.Logging;
using Presentation.Services;
using Serilog.Extensions.Logging;
using SmartLib.Tests.Helpers;
using SmartLib.Tests.TestData;

namespace SmartLib.Tests.Logging
{
    // Перевіряє налаштування Serilog: записи справді потрапляють у текстовий файл
    [Trait("Category", "Unit")]
    public sealed class AppLoggingTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"smartlib-logs-{Guid.NewGuid():N}");

        [Fact]
        public async Task FailedSignIn_IsWrittenToLogFile_WithoutPersonalData()
        {
            // Arrange
            using var db = TestDb.Create();
            TestDb.AddUser(db, UserTestData.ReaderEmail);

            // Act: dispose фабрики закриває файл і дописує буфер
            using (var factory = new SerilogLoggerFactory(AppLogging.CreateLogger(_directory), dispose: true))
            {
                var service = new AuthService(db, factory.CreateLogger<AuthService>());
                await service.AuthenticateAsync(UserTestData.ReaderEmail, UserTestData.WrongPassword);
            }

            // Assert
            var file = Assert.Single(Directory.GetFiles(_directory, "smartlib-*.log"));
            var text = await File.ReadAllTextAsync(file);
            Assert.Contains($"[WRN] {typeof(AuthService).FullName}", text);
            Assert.Contains("wrong password", text);
            Assert.DoesNotContain(UserTestData.ReaderEmail, text);
            Assert.DoesNotContain(UserTestData.WrongPassword, text);
        }

        [Fact]
        public void DefaultDirectory_IsOutsideTheRepository()
        {
            // Act
            var directory = AppLogging.DefaultDirectory;

            // Assert: журнал лежить у профілі користувача, а не поруч із кодом
            Assert.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), directory);
            Assert.EndsWith(Path.Combine("SmartLib", "logs"), directory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }
    }
}
