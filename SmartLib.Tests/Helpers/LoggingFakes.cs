using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SmartLib.Tests.Helpers
{
    // Логер для тестів: запам'ятовує всі записи, щоб перевірити рівень і текст повідомлення
    public sealed class ListLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    // Імітує збій бази під час SaveChanges: вмикається після підготовки тестових даних
    public sealed class ThrowOnSaveInterceptor : SaveChangesInterceptor
    {
        private readonly Exception _exception;

        public ThrowOnSaveInterceptor(Exception exception) => _exception = exception;

        public bool Enabled { get; set; }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
            Enabled ? throw _exception : result;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            Enabled ? throw _exception : ValueTask.FromResult(result);
    }
}
