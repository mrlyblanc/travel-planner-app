using System.Net;
using System.Net.Http.Json;
using TravelPlannerApp.Api.IntegrationTests.Support;
using TravelPlannerApp.Application.Contracts.Auth;

namespace TravelPlannerApp.Api.IntegrationTests;

public sealed class TransportSecurityTests
{
    [Fact]
    public async Task Production_HttpsResponse_IncludesHstsHeader()
    {
        using var factory = new TravelPlannerApiFactory("Production");
        using var client = factory.CreateApiClient("https://travelplannerapp.test");

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Production_HttpRequest_WithoutForwardedProto_ReturnsHttpsRequiredProblem()
    {
        using var factory = new TravelPlannerApiFactory("Production");
        using var client = factory.CreateApiClient("http://travelplannerapp.test");

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "ava.santos@globejet.com",
            Password = TravelPlannerApiFactory.SeedPassword
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("HTTPS Required", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_ForwardedHttpsRequest_IsAccepted()
    {
        using var factory = new TravelPlannerApiFactory("Production");
        using var client = factory.CreateApiClient("http://travelplannerapp.test");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-Proto", "https");

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "ava.santos@globejet.com",
            Password = TravelPlannerApiFactory.SeedPassword
        });

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.Contains("Strict-Transport-Security"));
    }
}
