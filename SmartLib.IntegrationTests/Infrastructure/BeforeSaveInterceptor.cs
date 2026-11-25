using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SmartLib.IntegrationTests.Infrastructure
{
    // Один раз виконує дію перед SaveChanges: так моделюємо зміну, яку інший користувач
    // встиг зберегти між перевірками сервісу і збереженням його даних
    public sealed class BeforeSaveInterceptor : SaveChangesInterceptor
    {
        private Func<Task>? _action;

        public BeforeSaveInterceptor(Func<Task> action) => _action = action;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var action = Interlocked.Exchange(ref _action, null);
            if (action is not null)
            {
                await action();
            }

            return result;
        }
    }
}
