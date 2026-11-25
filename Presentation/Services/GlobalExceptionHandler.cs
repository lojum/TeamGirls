using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;

namespace Presentation.Services;

// Показ повідомлення винесено в інтерфейс, щоб обробник можна було перевірити тестами без вікон
public interface IErrorNotifier
{
    void ShowError(string message);
}

public sealed class MessageBoxErrorNotifier : IErrorNotifier
{
    public void ShowError(string message) =>
        MessageBox.Show(message, "Unexpected error", MessageBoxButton.OK, MessageBoxImage.Error);
}

// Останній рубіж обробки помилок: виняток, який ніхто не перехопив, записується в журнал,
// а користувач бачить зрозуміле повідомлення замість аварійного закриття застосунку
public sealed class GlobalExceptionHandler
{
    public const string UserMessage =
        "Something went wrong. The error has been logged.\n\n" +
        "You can continue working. If the problem repeats, restart the application.";

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IErrorNotifier _notifier;
    private bool _isShowingMessage;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IErrorNotifier notifier)
    {
        _logger = logger;
        _notifier = notifier;
    }

    public void Register(Application application)
    {
        application.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    // Помилка в потоці інтерфейсу (команди, обробники подій вікон): застосунок продовжує роботу
    public bool HandleUiException(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception in the user interface");

        // Якщо помилка повторюється, поки відкрите попереднє повідомлення, не відкриваємо нове
        if (_isShowingMessage)
            return true;

        _isShowingMessage = true;
        try
        {
            _notifier.ShowError(UserMessage);
        }
        catch (Exception notifyException)
        {
            _logger.LogError(notifyException, "Could not show the error message to the user");
        }
        finally
        {
            _isShowingMessage = false;
        }

        return true;
    }

    // Помилка поза потоком інтерфейсу: .NET завершить процес, тому лише записуємо її
    public void HandleFatalException(object exceptionObject)
    {
        _logger.LogCritical(exceptionObject as Exception,
            "Fatal unhandled exception, the application will close: {Error}", exceptionObject.GetType().Name);
    }

    // Виняток у фоновому завданні, результат якого ніхто не дочекався
    public void HandleUnobservedTaskException(Exception exception)
    {
        _logger.LogError(exception, "Unobserved exception in a background task");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
        e.Handled = HandleUiException(e.Exception);

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        HandleFatalException(e.ExceptionObject);
        if (e.IsTerminating)
        {
            // Процес зараз завершиться: дописуємо журнал у файл
            Serilog.Log.CloseAndFlush();
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        HandleUnobservedTaskException(e.Exception);
        e.SetObserved();
    }
}
