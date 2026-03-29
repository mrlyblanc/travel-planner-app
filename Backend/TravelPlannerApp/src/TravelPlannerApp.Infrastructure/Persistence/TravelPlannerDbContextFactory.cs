using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TravelPlannerApp.Infrastructure.Persistence;

public sealed class TravelPlannerDbContextFactory : IDesignTimeDbContextFactory<TravelPlannerDbContext>
{
    public TravelPlannerDbContext CreateDbContext(string[] args)
    {
        LoadDotEnv();

        var apiProjectPath = ResolveApiProjectPath();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        if (!string.IsNullOrWhiteSpace(environment))
        {
            configurationBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true);
        }

        var configuration = configurationBuilder.Build();
        var optionsBuilder = new DbContextOptionsBuilder<TravelPlannerDbContext>();
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("ConnectionStrings:SqlServer is missing.");
        EnsureDatabaseNameExists(connectionString, "ConnectionStrings:SqlServer");
        optionsBuilder.UseSqlServer(connectionString);

        return new TravelPlannerDbContext(optionsBuilder.Options);
    }

    private static void LoadDotEnv()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var envPath = Path.Combine(current.FullName, ".env");
            if (File.Exists(envPath))
            {
                foreach (var line in File.ReadLines(envPath))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                    {
                        continue;
                    }

                    var index = trimmed.IndexOf('=');
                    if (index <= 0)
                    {
                        continue;
                    }

                    var key = trimmed[..index].Trim();
                    var value = trimmed[(index + 1)..].Trim().Trim('"');
                    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, value);
                    }
                }

                return;
            }

            current = current.Parent;
        }
    }

    private static string ResolveApiProjectPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "TravelPlannerApp.Api");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the API project directory.");
    }

    private static void EnsureDatabaseNameExists(string connectionString, string settingName)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        var databaseKey = builder.Keys
            .Cast<string>()
            .FirstOrDefault(static key =>
                key.Equals("Database", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase));

        if (databaseKey is null || string.IsNullOrWhiteSpace(builder[databaseKey]?.ToString()))
        {
            throw new InvalidOperationException($"{settingName} must include a database name.");
        }
    }
}
