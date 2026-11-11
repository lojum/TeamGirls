using BusinessLogic.Services;
using DataAccess;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BusinessLogic;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddSingleton(_ => CreateDataSource(connectionString));

        services.AddDbContext<SmartLibDbContext>((serviceProvider, options) =>
            options.UseSmartLibDatabase(serviceProvider.GetRequiredService<NpgsqlDataSource>()));

        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<ILibrarianDashboardService, LibrarianDashboardService>();

        return services;
    }

    // Спільні налаштування підключення: їх використовують і застосунок, і інтеграційні тести
    public static NpgsqlDataSource CreateDataSource(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<UserRole>("user_role");
        dataSourceBuilder.MapEnum<CopyStatus>("copy_status");
        dataSourceBuilder.MapEnum<ReservationStatus>("reservation_status");
        return dataSourceBuilder.Build();
    }

    public static TBuilder UseSmartLibDatabase<TBuilder>(this TBuilder options, NpgsqlDataSource dataSource)
        where TBuilder : DbContextOptionsBuilder
    {
        options.UseNpgsql(dataSource).UseSnakeCaseNamingConvention();
        return options;
    }
}
