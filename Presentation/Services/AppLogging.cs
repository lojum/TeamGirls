using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Presentation.Services;

// Журнал застосунку: текстовий файл на кожен день у %LOCALAPPDATA%\SmartLib\logs (поза репозиторієм)
public static class AppLogging
{
    public const string FileNamePattern = "smartlib-.log";
    public const int RetainedDays = 14;

    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartLib", "logs");

    public static Logger CreateLogger(string directory) =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            // Службові повідомлення EF Core і .NET (зокрема тексти SQL-запитів) пишемо лише від рівня Warning
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(directory, FileNamePattern),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: RetainedDays,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
}
