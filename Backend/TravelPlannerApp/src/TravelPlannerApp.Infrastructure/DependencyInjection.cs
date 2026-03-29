using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Infrastructure.Persistence;
using TravelPlannerApp.Infrastructure.Persistence.Repositories;
using TravelPlannerApp.Infrastructure.Persistence.Seed;

namespace TravelPlannerApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        var provider = (configuration["Database:Provider"] ?? "MySql").Trim();

        services.AddDbContext<TravelPlannerDbContext>((_, options) =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                ConfigureSqlServer(options, configuration);
            }
            else
            {
                ConfigureMySql(options, configuration);
            }

            if (isDevelopment)
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IItineraryRepository, ItineraryRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUnitOfWork>(static serviceProvider => serviceProvider.GetRequiredService<TravelPlannerDbContext>());
        services.AddScoped<TravelPlannerDbSeeder>();

        return services;
    }

    public static async Task InitialiseDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TravelPlannerDbContext>();

        if (dbContext.Database.ProviderName is "Microsoft.EntityFrameworkCore.SqlServer" or "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            await dbContext.Database.MigrateAsync();
        }

        var seeder = scope.ServiceProvider.GetRequiredService<TravelPlannerDbSeeder>();
        await seeder.SeedAsync();
    }

    private static void ConfigureMySql(DbContextOptionsBuilder options, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("ConnectionStrings:MySql is missing.");

        EnsureDatabaseNameExists(connectionString, "ConnectionStrings:MySql");

        var serverVersion = configuration["Database:ServerVersion"] ?? "8.0.36";
        options.UseMySql(
            connectionString,
            new MySqlServerVersion(Version.Parse(serverVersion)),
            mysql => mysql.EnableRetryOnFailure());
    }

    private static void ConfigureSqlServer(DbContextOptionsBuilder options, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is missing.");

        EnsureDatabaseNameExists(connectionString, "ConnectionStrings:SqlServer");

        options.UseSqlServer(connectionString, sqlServer => sqlServer.EnableRetryOnFailure());
    }

    private static void EnsureDatabaseNameExists(string connectionString, string settingName)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        var databaseValue = builder.Keys
            .Cast<string>()
            .FirstOrDefault(static key =>
                key.Equals("Database", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase));

        if (databaseValue is null || string.IsNullOrWhiteSpace(builder[databaseValue]?.ToString()))
        {
            throw new InvalidOperationException($"{settingName} must include a database name.");
        }
    }
}
