using System.Data.Common;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TravelPlannerApp.Application.Contracts.Auth;
using TravelPlannerApp.Infrastructure.Persistence;

namespace TravelPlannerApp.Api.IntegrationTests.Support;

public sealed class TravelPlannerApiFactory : WebApplicationFactory<Program>
{
    public const string SeedPassword = "Travel123!";
    private readonly string _environmentName;

    public TravelPlannerApiFactory(string environmentName = "Testing")
    {
        _environmentName = environmentName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ApplyMigrationsOnStartup"] = "true",
                ["Jwt:Issuer"] = "TravelPlannerApp.Tests",
                ["Jwt:Audience"] = "TravelPlannerApp.Tests.Client",
                ["Jwt:Secret"] = "integration-tests-secret-key-1234567890",
                ["Jwt:TokenLifetimeMinutes"] = "120",
                ["Jwt:RefreshTokenLifetimeDays"] = "14",
                ["Seed:Enabled"] = "true",
                ["Seed:DefaultUserPassword"] = SeedPassword,
                ["TransportSecurity:EnforceHttpsInProduction"] = "true",
                ["TransportSecurity:Hsts:MaxAgeDays"] = "365",
                ["TransportSecurity:Hsts:IncludeSubDomains"] = "false",
                ["TransportSecurity:Hsts:Preload"] = "false"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TravelPlannerDbContext>();
            services.RemoveAll<DbContextOptions<TravelPlannerDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TravelPlannerDbContext>>();
            services.RemoveAll<DbConnection>();

            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            services.AddSingleton<DbConnection>(connection);
            services.AddDbContext<TravelPlannerDbContext>((serviceProvider, options) =>
            {
                var dbConnection = serviceProvider.GetRequiredService<DbConnection>();
                options.UseSqlite(dbConnection);
            });
        });
    }

    public HttpClient CreateApiClient(string baseAddress = "https://localhost")
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri(baseAddress)
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string baseAddress = "https://localhost")
    {
        var client = CreateApiClient(baseAddress);
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = SeedPassword
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthSessionResponse>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }
}
