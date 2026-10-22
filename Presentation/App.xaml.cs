using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DataAccess;
using Domain.Enums;
using Npgsql;

namespace Presentation
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            string connectionString = "Host=aws-0-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pyzxothzgzwdzdhwapjb;Password=databasesstrongpasswor;Ssl Mode=Require;Trust Server Certificate=true;";

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

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}