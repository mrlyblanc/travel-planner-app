using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TravelPlannerApp.Infrastructure.DatabaseInitialiser");

        logger.LogInformation("Initialising database with provider {ProviderName}", dbContext.Database.ProviderName);

        if (dbContext.Database.ProviderName is "Microsoft.EntityFrameworkCore.SqlServer" or "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await dbContext.Database.EnsureCreatedAsync();
            await EnsureSqlServerConcurrencyColumnsAsync(dbContext, logger);
        }
        else
        {
            await dbContext.Database.MigrateAsync();
        }

        var seeder = scope.ServiceProvider.GetRequiredService<TravelPlannerDbSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Database initialisation complete");
    }

    private static async Task EnsureSqlServerConcurrencyColumnsAsync(TravelPlannerDbContext dbContext, ILogger logger)
    {
        if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.SqlServer")
        {
            return;
        }

        logger.LogInformation("Ensuring SQL Server concurrency columns are present");
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[users]', N'U') IS NOT NULL AND COL_LENGTH('users', 'ConcurrencyToken') IS NULL
            BEGIN
                ALTER TABLE [users] ADD [ConcurrencyToken] nvarchar(40) NOT NULL CONSTRAINT [DF_users_ConcurrencyToken] DEFAULT '';
                UPDATE [users] SET [ConcurrencyToken] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')) WHERE [ConcurrencyToken] = '';
                ALTER TABLE [users] DROP CONSTRAINT [DF_users_ConcurrencyToken];
            END;

            IF OBJECT_ID(N'[itineraries]', N'U') IS NOT NULL AND COL_LENGTH('itineraries', 'ConcurrencyToken') IS NULL
            BEGIN
                ALTER TABLE [itineraries] ADD [ConcurrencyToken] nvarchar(40) NOT NULL CONSTRAINT [DF_itineraries_ConcurrencyToken] DEFAULT '';
                UPDATE [itineraries] SET [ConcurrencyToken] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')) WHERE [ConcurrencyToken] = '';
                ALTER TABLE [itineraries] DROP CONSTRAINT [DF_itineraries_ConcurrencyToken];
            END;

            IF OBJECT_ID(N'[events]', N'U') IS NOT NULL AND COL_LENGTH('events', 'ConcurrencyToken') IS NULL
            BEGIN
                ALTER TABLE [events] ADD [ConcurrencyToken] nvarchar(40) NOT NULL CONSTRAINT [DF_events_ConcurrencyToken] DEFAULT '';
                UPDATE [events] SET [ConcurrencyToken] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')) WHERE [ConcurrencyToken] = '';
                ALTER TABLE [events] DROP CONSTRAINT [DF_events_ConcurrencyToken];
            END;
            """);
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
