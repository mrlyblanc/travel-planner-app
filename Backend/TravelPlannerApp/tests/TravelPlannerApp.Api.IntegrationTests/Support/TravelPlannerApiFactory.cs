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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "TravelPlannerApp.Tests",
                ["Jwt:Audience"] = "TravelPlannerApp.Tests.Client",
                ["Jwt:Secret"] = "integration-tests-secret-key-1234567890",
                ["Jwt:TokenLifetimeMinutes"] = "120",
                ["Seed:DefaultUserPassword"] = SeedPassword
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

    public HttpClient CreateApiClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = CreateApiClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = SeedPassword
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }
}
