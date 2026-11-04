using System;
using System.IO;
using System.Windows;
using BusinessLogic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Services;
using Presentation.ViewModels;
using Presentation.Views;

namespace Presentation
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public static IConfiguration Configuration { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Замінили GetCurrentDirectory на BaseDirectory - це найбезпечніший шлях у WPF
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                
                Configuration = builder.Build();

                var services = new ServiceCollection();
                services.AddBusinessLogic(Configuration);
                services.AddSingleton<IWindowNavigationService, WindowNavigationService>();
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
                ServiceProvider.GetRequiredService<IWindowNavigationService>().ShowMain();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критична помилка при запуску:\n{ex.Message}", "Помилка запуску", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            (ServiceProvider as IDisposable)?.Dispose();
            base.OnExit(e);
        }
    }
}