using Microsoft.Extensions.Logging;
using Presentation.Services;
using SmartLib.Tests.Helpers;
using static SmartLib.Tests.TestData.ErrorHandlingTestData;

namespace SmartLib.Tests.Logging
{
    [Trait("Category", "Unit")]
    public class GlobalExceptionHandlerTests
    {
        private readonly ListLogger<GlobalExceptionHandler> _logger = new();
        private readonly FakeErrorNotifier _notifier = new();

        private GlobalExceptionHandler CreateHandler() => new(_logger, _notifier);

        [Fact]
        public void HandleUiException_LogsErrorAndShowsFriendlyMessage()
        {
            // Arrange
            var exception = new InvalidOperationException(TechnicalMessage);

            // Act
            var handled = CreateHandler().HandleUiException(exception);

            // Assert: застосунок не закривається, виняток у журналі, користувач бачить зрозумілий текст
            Assert.True(handled);
            var entry = Assert.Single(_logger.Entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Same(exception, entry.Exception);
            var message = Assert.Single(_notifier.Messages);
            Assert.Equal(GlobalExceptionHandler.UserMessage, message);
            Assert.DoesNotContain(TechnicalMessage, message);
        }

        [Fact]
        public void HandleUiException_NotifierFails_StillHandledAndLogged()
        {
            // Arrange
            _notifier.ExceptionToThrow = new InvalidOperationException("window is closing");

            // Act
            var handled = CreateHandler().HandleUiException(new InvalidOperationException(TechnicalMessage));

            // Assert
            Assert.True(handled);
            Assert.Equal(2, _logger.Entries.Count(e => e.Level == LogLevel.Error));
        }

        // Повторна помилка, поки відкрите повідомлення, не відкриває друге вікно, але теж записується
        [Fact]
        public void HandleUiException_WhileMessageIsShown_DoesNotShowSecondMessage()
        {
            // Arrange
            var handler = CreateHandler();
            _notifier.OnShow = () => handler.HandleUiException(new InvalidOperationException("second"));

            // Act
            handler.HandleUiException(new InvalidOperationException(TechnicalMessage));

            // Assert
            Assert.Single(_notifier.Messages);
            Assert.Equal(2, _logger.Entries.Count);
        }

        [Fact]
        public void HandleFatalException_LogsCritical()
        {
            // Arrange
            var exception = new OutOfMemoryException();

            // Act
            CreateHandler().HandleFatalException(exception);

            // Assert
            var entry = Assert.Single(_logger.Entries);
            Assert.Equal(LogLevel.Critical, entry.Level);
            Assert.Same(exception, entry.Exception);
            Assert.Empty(_notifier.Messages);
        }

        [Fact]
        public void HandleUnobservedTaskException_LogsError()
        {
            // Arrange
            var exception = new AggregateException(new TimeoutException());

            // Act
            CreateHandler().HandleUnobservedTaskException(exception);

            // Assert
            var entry = Assert.Single(_logger.Entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Same(exception, entry.Exception);
        }
    }
}
