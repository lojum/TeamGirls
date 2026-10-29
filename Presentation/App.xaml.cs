using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DataAccess;
using Domain.Enums;
using Npgsql;
using Presentation.Views;
using BusinessLogic.Services;

namespace Presentation
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; } = null!;
        public static IConfiguration Configuration { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                
                Configuration = builder.Build();
                string connectionString = Configuration.GetConnectionString("DefaultConnection")!;

                var services = new ServiceCollection();

                services.AddSingleton(_ =>
                {
                    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
                    dataSourceBuilder.MapEnum<UserRole>("user_role");
                    dataSourceBuilder.MapEnum<CopyStatus>("copy_status");
                    dataSourceBuilder.MapEnum<ReservationStatus>("reservation_status");
                    return dataSourceBuilder.Build();
                });

                services.AddDbContext<SmartLibDbContext>((serviceProvider, options) =>
                    options.UseNpgsql(serviceProvider.GetRequiredService<NpgsqlDataSource>())
                           .UseSnakeCaseNamingConvention() 
                );

                services.AddSingleton(TimeProvider.System);
                services.AddScoped<IAuthService, AuthService>();
                services.AddScoped<IBookService, BookService>();
                services.AddScoped<ILoanService, LoanService>();

                ServiceProvider = services.BuildServiceProvider();

                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Critical error on startup:\n{ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }
    }
}