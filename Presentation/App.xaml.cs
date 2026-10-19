using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DataAccess;

namespace Presentation
{
    public partial class App : Application
    {
        public static ServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            string connectionString = "Host=aws-0-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.pyzxothzgzwdzdhwapjb;Password=databasesstrongpasswor;Ssl Mode=Require;Trust Server Certificate=true;";

            services.AddDbContext<SmartLibDbContext>(options =>
                options.UseNpgsql(connectionString)
                       .UseSnakeCaseNamingConvention() 
            );

            ServiceProvider = services.BuildServiceProvider();
        }
    }
}