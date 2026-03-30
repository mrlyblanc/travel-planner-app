using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TravelPlannerApp.Application.Abstractions.Security;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Infrastructure.Authentication;
using TravelPlannerApp.Infrastructure.Persistence;
using TravelPlannerApp.Infrastructure.Persistence.Repositories;
using TravelPlannerApp.Infrastructure.Persistence.Seed;

namespace TravelPlannerApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        services.AddDbContext<TravelPlannerDbContext>((_, options) =>
        {
            ConfigureSqlServer(options, configuration);

            if (isDevelopment)
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IItineraryRepository, ItineraryRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<IUnitOfWork>(static serviceProvider => serviceProvider.GetRequiredService<TravelPlannerDbContext>());
        services.AddScoped<TravelPlannerDbSeeder>();

        return services;
    }

    public static async Task InitialiseDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TravelPlannerDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("TravelPlannerApp.Infrastructure.DatabaseInitialiser");

        logger.LogInformation("Initialising database with provider {ProviderName}", dbContext.Database.ProviderName);

        if (!IsEnabled(configuration["Database:ApplyMigrationsOnStartup"]))
        {
            logger.LogInformation("Database startup initialization is disabled");
            return;
        }

        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        else
        {
            await InitialiseSqlServerDatabaseAsync(dbContext, logger);
        }

        await BackfillMissingPasswordHashesAsync(dbContext, configuration, passwordHasher, logger);
        await BackfillMissingAuthVersionsAsync(dbContext, logger);

        var seeder = scope.ServiceProvider.GetRequiredService<TravelPlannerDbSeeder>();
        await seeder.SeedAsync();
        logger.LogInformation("Database initialisation complete");
    }

    private static async Task InitialiseSqlServerDatabaseAsync(TravelPlannerDbContext dbContext, ILogger logger)
    {
        if (!await dbContext.Database.CanConnectAsync())
        {
            logger.LogInformation("SQL Server database not found. Applying migrations.");
            await dbContext.Database.MigrateAsync();
            return;
        }

        var hasApplicationTables = await SqlServerTableExistsAsync(dbContext, "users")
            || await SqlServerTableExistsAsync(dbContext, "itineraries")
            || await SqlServerTableExistsAsync(dbContext, "events");

        if (!hasApplicationTables)
        {
            logger.LogInformation("SQL Server database is empty. Applying migrations.");
            await dbContext.Database.MigrateAsync();
            return;
        }

        var hasMigrationHistory = await SqlServerTableExistsAsync(dbContext, "__EFMigrationsHistory");
        if (hasMigrationHistory)
        {
            logger.LogInformation("SQL Server migration history detected. Applying migrations.");
            await dbContext.Database.MigrateAsync();
            return;
        }

        logger.LogWarning("Legacy SQL Server schema detected without migration history. Applying compatibility upgrades and baselining migrations.");
        await EnsureSqlServerCompatibilityColumnsAsync(dbContext, logger);
        await BaselineSqlServerMigrationHistoryAsync(dbContext, logger);
        await dbContext.Database.MigrateAsync();
    }

    private static async Task EnsureSqlServerCompatibilityColumnsAsync(TravelPlannerDbContext dbContext, ILogger logger)
    {
        if (dbContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.SqlServer")
        {
            return;
        }

        logger.LogInformation("Ensuring SQL Server concurrency columns are present");
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'[dbo].[users]', N'U') IS NOT NULL AND COL_LENGTH('dbo.users', 'ConcurrencyToken') IS NULL
            BEGIN
                ALTER TABLE [dbo].[users] ADD [ConcurrencyToken] nvarchar(40) NOT NULL CONSTRAINT [DF_users_ConcurrencyToken] DEFAULT '';
                UPDATE [dbo].[users] SET [ConcurrencyToken] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')) WHERE [ConcurrencyToken] = '';
                ALTER TABLE [dbo].[users] DROP CONSTRAINT [DF_users_ConcurrencyToken];
            END;

            IF OBJECT_ID(N'[dbo].[users]', N'U') IS NOT NULL AND COL_LENGTH('dbo.users', 'PasswordHash') IS NULL
            BEGIN
                ALTER TABLE [dbo].[users] ADD [PasswordHash] nvarchar(512) NOT NULL CONSTRAINT [DF_users_PasswordHash] DEFAULT '';
                ALTER TABLE [dbo].[users] DROP CONSTRAINT [DF_users_PasswordHash];
            END;

            IF OBJECT_ID(N'[dbo].[users]', N'U') IS NOT NULL AND COL_LENGTH('dbo.users', 'AuthVersion') IS NULL
            BEGIN
                ALTER TABLE [dbo].[users] ADD [AuthVersion] nvarchar(40) NOT NULL CONSTRAINT [DF_users_AuthVersion] DEFAULT '';
                UPDATE [dbo].[users] SET [AuthVersion] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')) WHERE [AuthVersion] = '';
                ALTER TABLE [dbo].[users] DROP CONSTRAINT [DF_users_AuthVersion];
            END;

            IF OBJECT_ID(N'[dbo].[itineraries]', N'U') IS NOT NULL AND COL_LENGTH('dbo.itineraries', 'ConcurrencyToken') IS NULL
            BEGIN
                ALTER TABLE [dbo].[itineraries] ADD [ConcurrencyToken] nvarchar(40) NOT NULL CONSTRAINT [DF_itineraries_ConcurrencyToken] DEFAULT '';
                UPDATE [dbo].[itineraries] SET [ConcurrencyToken] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')) WHERE [ConcurrencyToken] = '';
                ALTER TABLE [dbo].[itineraries] DROP CONSTRAINT [DF_itineraries_ConcurrencyToken];
            END;

            IF OBJECT_ID(N'[dbo].[events]', N'U') IS NOT NULL AND COL_LENGTH('dbo.events', 'ConcurrencyToken') IS NULL
            BEGIN
                ALTER TABLE [dbo].[events] ADD [ConcurrencyToken] nvarchar(40) NOT NULL CONSTRAINT [DF_events_ConcurrencyToken] DEFAULT '';
                UPDATE [dbo].[events] SET [ConcurrencyToken] = LOWER(REPLACE(CONVERT(varchar(36), NEWID()), '-', '')) WHERE [ConcurrencyToken] = '';
                ALTER TABLE [dbo].[events] DROP CONSTRAINT [DF_events_ConcurrencyToken];
            END;
            """);
    }

    private static async Task<bool> SqlServerTableExistsAsync(TravelPlannerDbContext dbContext, string tableName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM sys.tables AS tables
                    INNER JOIN sys.schemas AS schemas ON tables.schema_id = schemas.schema_id
                    WHERE schemas.name = 'dbo' AND tables.name = @tableName
                ) THEN 1 ELSE 0 END
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return result is int intValue && intValue == 1;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task BaselineSqlServerMigrationHistoryAsync(TravelPlannerDbContext dbContext, ILogger logger)
    {
        var migrations = dbContext.Database.GetMigrations().ToList();
        if (migrations.Count == 0)
        {
            return;
        }

        var initialMigrationId = migrations[0];
        var productVersion = typeof(DbContext).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .Split('+')[0]
            ?? "9.0.0";

        logger.LogInformation("Baselining SQL Server migration history with migration {MigrationId}", initialMigrationId);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[__EFMigrationsHistory] (
                    [MigrationId] nvarchar(150) NOT NULL,
                    [ProductVersion] nvarchar(32) NOT NULL,
                    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                );
            END;

            IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'{initialMigrationId}')
            BEGIN
                INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                VALUES (N'{initialMigrationId}', N'{productVersion}');
            END;
            """);
    }

    private static async Task BackfillMissingPasswordHashesAsync(
        TravelPlannerDbContext dbContext,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        ILogger logger)
    {
        var seedPassword = configuration["Seed:DefaultUserPassword"]?.Trim();
        if (string.IsNullOrWhiteSpace(seedPassword) || string.Equals(seedPassword, "__set_in_env__", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var usersWithMissingPasswords = await dbContext.Users
            .Where(user => string.IsNullOrWhiteSpace(user.PasswordHash))
            .ToListAsync();

        if (usersWithMissingPasswords.Count == 0)
        {
            return;
        }

        logger.LogInformation("Backfilling password hashes for {UserCount} existing users", usersWithMissingPasswords.Count);
        foreach (var user in usersWithMissingPasswords)
        {
            user.PasswordHash = passwordHasher.HashPassword(seedPassword);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task BackfillMissingAuthVersionsAsync(
        TravelPlannerDbContext dbContext,
        ILogger logger)
    {
        var usersWithMissingAuthVersion = await dbContext.Users
            .Where(user => string.IsNullOrWhiteSpace(user.AuthVersion))
            .ToListAsync();

        if (usersWithMissingAuthVersion.Count == 0)
        {
            return;
        }

        logger.LogInformation("Backfilling auth versions for {UserCount} existing users", usersWithMissingAuthVersion.Count);
        foreach (var user in usersWithMissingAuthVersion)
        {
            user.AuthVersion = Guid.NewGuid().ToString("N");
        }

        await dbContext.SaveChangesAsync();
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

    private static bool IsEnabled(string? rawValue)
    {
        return bool.TryParse(rawValue, out var enabled) && enabled;
    }
}
