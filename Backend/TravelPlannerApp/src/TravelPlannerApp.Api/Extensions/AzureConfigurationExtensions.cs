using System.Collections;
using Azure.Identity;

namespace TravelPlannerApp.Api.Extensions;

public static class AzureConfigurationExtensions
{
    private static readonly string[] AppServiceConnectionStringPrefixes =
    [
        "SQLCONNSTR_",
        "SQLAZURECONNSTR_",
        "MYSQLCONNSTR_",
        "POSTGRESQLCONNSTR_",
        "CUSTOMCONNSTR_"
    ];

    public static WebApplicationBuilder AddAzureConfiguration(this WebApplicationBuilder builder, string[] args)
    {
        var keyVaultUriValue = builder.Configuration["Azure:KeyVault:VaultUri"]?.Trim();
        if (Uri.TryCreate(keyVaultUriValue, UriKind.Absolute, out var keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(keyVaultUri, CreateCredential(builder.Configuration));
        }

        var appServiceConnectionStrings = GetAzureAppServiceConnectionStrings();
        if (appServiceConnectionStrings.Count > 0)
        {
            builder.Configuration.AddInMemoryCollection(appServiceConnectionStrings);
        }

        builder.Configuration.AddEnvironmentVariables();

        if (args.Length > 0)
        {
            builder.Configuration.AddCommandLine(args);
        }

        return builder;
    }

    private static DefaultAzureCredential CreateCredential(IConfiguration configuration)
    {
        var options = new DefaultAzureCredentialOptions();
        var managedIdentityClientId = configuration["Azure:ManagedIdentityClientId"]?.Trim();
        if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
        {
            options.ManagedIdentityClientId = managedIdentityClientId;
        }

        return new DefaultAzureCredential(options);
    }

    private static Dictionary<string, string?> GetAzureAppServiceConnectionStrings()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var environmentVariables = Environment.GetEnvironmentVariables();

        foreach (DictionaryEntry entry in environmentVariables)
        {
            if (entry.Key is not string key || entry.Value is null)
            {
                continue;
            }

            var prefix = AppServiceConnectionStringPrefixes.FirstOrDefault(prefix =>
                key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (prefix is null)
            {
                continue;
            }

            var name = key[prefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            overrides[$"ConnectionStrings:{name}"] = entry.Value.ToString();
        }

        return overrides;
    }
}
