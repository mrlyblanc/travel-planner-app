using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using TravelPlannerApp.Api.Common.Authorization;
using TravelPlannerApp.Api.Common.CurrentUser;
using TravelPlannerApp.Api.Common.Security;
using TravelPlannerApp.Api.IntegrationTests.Support;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Contracts.Users;

namespace TravelPlannerApp.Api.IntegrationTests;

public sealed class JwtOwnershipTests
{
    private const string AvaEmail = "ava.santos@globejet.com";
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    [Fact]
    public void GetCurrentUserId_WhenSubClaimExists_ReturnsSubClaimValue()
    {
        var principal = CreatePrincipal(new Claim("sub", "user-ava"));

        var currentUserId = principal.GetCurrentUserId();

        Assert.Equal("user-ava", currentUserId);
    }

    [Fact]
    public async Task StringResourceOwnerHandler_WhenOnlySubClaimExists_Succeeds()
    {
        var principal = CreatePrincipal(new Claim("sub", "user-ava"));
        var requirement = new ResourceOwnerRequirement();
        var context = new AuthorizationHandlerContext([requirement], principal, "user-ava");
        var handler = new StringResourceOwnerHandler();

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void ClaimsCurrentUserAccessor_WhenNameIdClaimExists_ReturnsClaimValue()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = CreatePrincipal(new Claim("nameid", "user-ava"))
            }
        };

        var accessor = new ClaimsCurrentUserAccessor(httpContextAccessor);

        Assert.Equal("user-ava", accessor.GetCurrentUserId());
    }

    [Fact]
    public async Task UpdateUser_WhenTargetingOwnProfileWithJwt_ReturnsUpdatedUser()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);
        var staleEtag = await GetEtagAsync(client, "/api/users/user-ava");

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/users/user-ava", new UpdateUserRequest
        {
            Name = "Ava Santos Updated",
            Email = AvaEmail,
            Avatar = "AU"
        }, staleEtag));

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.NotNull(user);
        Assert.Equal("Ava Santos Updated", user!.Name);
    }

    [Fact]
    public async Task UpdateItinerary_WhenUserOwnsItineraryWithJwt_ReturnsUpdatedItinerary()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);
        var staleEtag = await GetEtagAsync(client, "/api/itineraries/itinerary-tokyo");

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/itineraries/itinerary-tokyo", new UpdateItineraryRequest
        {
            Title = "Tokyo Food Sprint",
            Destination = "Tokyo",
            Description = "Owner update",
            StartDate = new DateOnly(2026, 4, 14),
            EndDate = new DateOnly(2026, 4, 18)
        }, staleEtag));

        response.EnsureSuccessStatusCode();
        var itinerary = await response.Content.ReadFromJsonAsync<ItineraryResponse>(JsonOptions);
        Assert.NotNull(itinerary);
        Assert.Equal("Tokyo Food Sprint", itinerary!.Title);
    }

    [Fact]
    public async Task SwaggerJson_ForSecuredEndpoints_IncludesBearerSecurityRequirement()
    {
        using var factory = new TravelPlannerApiFactory("Development");
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"Bearer\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"security\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthMe_WithMalformedBearerToken_ReturnsUnauthorized()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private static HttpRequestMessage CreateIfMatchRequest(HttpMethod method, string path, object body, string etag)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return request;
    }

    private static async Task<string> GetEtagAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var etag = response.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrWhiteSpace(etag));
        return etag!;
    }
}
