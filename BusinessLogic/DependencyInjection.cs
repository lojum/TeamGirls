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
                .UseSnakeCaseNamingConvention());

        services.AddScoped<IBookService, BookService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();

        return services;
    }
}
