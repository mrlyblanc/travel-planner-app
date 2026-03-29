using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TravelPlannerApp.Infrastructure.Persistence;

namespace TravelPlannerApp.Api.IntegrationTests.Support;

public sealed class TravelPlannerApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
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
}
