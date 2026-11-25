using System;
using System.IO;
using System.Windows;
using BusinessLogic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Presentation.Services;
using Presentation.ViewModels;
using Presentation.Views;

namespace Presentation
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public static IConfiguration Configuration { get; private set; } = null!;

        private const string ConnectionStringName = "DefaultConnection";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Журнал налаштовуємо першим, щоб записати навіть збій під час запуску
            Log.Logger = AppLogging.CreateLogger(AppLogging.DefaultDirectory);
            Log.Information("SmartLib is starting");

            try
            {
                // Пароль бази не зберігається в репозиторії: рядок підключення береться з user-secrets
                // (або зі змінної середовища SMARTLIB_ConnectionStrings__DefaultConnection)
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddUserSecrets<App>(optional: true)
                    .AddEnvironmentVariables(prefix: "SMARTLIB_");

                Configuration = builder.Build();

                if (string.IsNullOrWhiteSpace(Configuration.GetConnectionString(ConnectionStringName)))
                {
                    Log.Warning("Startup stopped: connection string {Name} is not configured", ConnectionStringName);
                    MessageBox.Show(
                        "The database connection string is not configured.\n\n" +
                        "Run this command in the project folder (ask the team for the connection string):\n\n" +
                        $"dotnet user-secrets set \"ConnectionStrings:{ConnectionStringName}\" \"<connection string>\" --project Presentation\n\n" +
                        "See README.md, section \"Database connection\".",
                        "Database is not configured", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Current.Shutdown();
                    return;
                }

                var services = new ServiceCollection();
                services.AddLogging(logging => logging.AddSerilog(dispose: false));
                services.AddBusinessLogic(Configuration);
                services.AddSingleton<IWindowNavigationService, WindowNavigationService>();
                services.AddSingleton<IErrorNotifier, MessageBoxErrorNotifier>();
                services.AddSingleton<GlobalExceptionHandler>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<LoginViewModel>();
                services.AddTransient<RegistrationViewModel>();
                services.AddTransient<AdminDashboardViewModel>();
                services.AddTransient<LibrarianDashboardViewModel>();
                services.AddTransient<ReaderDashboardViewModel>();
                services.AddTransient<MainWindow>();
                services.AddTransient<LoginWindow>();
                services.AddTransient<RegistrationWindow>();
                services.AddTransient<LibrarianDashboardWindow>();

                ServiceProvider = services.BuildServiceProvider();

                // Від цього моменту будь-який неперехоплений виняток записується в журнал і не закриває застосунок
                ServiceProvider.GetRequiredService<GlobalExceptionHandler>().Register(this);
                ServiceProvider.GetRequiredService<IWindowNavigationService>().ShowMain();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "SmartLib failed to start");
                MessageBox.Show($"Critical error on startup:\n{ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            (ServiceProvider as IDisposable)?.Dispose();
            Log.Information("SmartLib stopped");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}