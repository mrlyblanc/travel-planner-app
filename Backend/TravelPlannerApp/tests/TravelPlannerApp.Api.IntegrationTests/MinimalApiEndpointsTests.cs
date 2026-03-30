using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TravelPlannerApp.Api.IntegrationTests.Support;
using TravelPlannerApp.Application.Contracts.Auth;
using TravelPlannerApp.Application.Contracts.Events;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Api.IntegrationTests;

public sealed class MinimalApiEndpointsTests
{
    private const string AvaEmail = "ava.santos@globejet.com";
    private const string LucaEmail = "luca.reyes@globejet.com";
    private const string MinaEmail = "mina.park@globejet.com";
    private const string NoahEmail = "noah.tan@globejet.com";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GetUsers_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/api/users?query=av");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithSeededCredentials_ReturnsJwtUserAndHttpOnlyRefreshCookie()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = AvaEmail,
            Password = TravelPlannerApiFactory.SeedPassword
        }, JsonOptions);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthSessionResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        Assert.Equal("Bearer", payload.TokenType);
        Assert.Equal("user-ava", payload.User.Id);
        Assert.Contains(
            response.Headers.TryGetValues("Set-Cookie", out var setCookieValues) ? setCookieValues : [],
            value => value.Contains("travelplanner.refresh=", StringComparison.Ordinal)
                && value.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_WhenRateLimitExceeded_ReturnsTooManyRequests()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = AvaEmail,
                Password = "wrong-password"
            }, JsonOptions);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var limitedResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = AvaEmail,
            Password = "wrong-password"
        }, JsonOptions);

        Assert.Equal((HttpStatusCode)429, limitedResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsRotatedTokens()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = AvaEmail,
            Password = TravelPlannerApiFactory.SeedPassword
        }, JsonOptions);
        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthSessionResponse>(JsonOptions);
        Assert.NotNull(loginPayload);

        var firstSetCookie = loginResponse.Headers.GetValues("Set-Cookie").Single();

        var refreshResponse = await client.PostAsync("/api/auth/refresh", content: null);

        refreshResponse.EnsureSuccessStatusCode();
        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthSessionResponse>(JsonOptions);
        Assert.NotNull(refreshPayload);
        Assert.NotEqual(loginPayload.AccessToken, refreshPayload!.AccessToken);
        var secondSetCookie = refreshResponse.Headers.GetValues("Set-Cookie").Single();
        Assert.NotEqual(firstSetCookie, secondSetCookie);
    }

    [Fact]
    public async Task Refresh_WhenReusingRefreshToken_ReturnsUnauthorized()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = AvaEmail,
            Password = TravelPlannerApiFactory.SeedPassword
        }, JsonOptions);
        loginResponse.EnsureSuccessStatusCode();
        Assert.NotNull(await loginResponse.Content.ReadFromJsonAsync<AuthSessionResponse>(JsonOptions));

        var issuedCookie = loginResponse.Headers.GetValues("Set-Cookie").Single();

        var firstRefreshResponse = await client.PostAsync("/api/auth/refresh", content: null);
        firstRefreshResponse.EnsureSuccessStatusCode();

        using var replayClient = factory.CreateApiClient();
        replayClient.DefaultRequestHeaders.Add("Cookie", issuedCookie.Split(';', 2)[0]);

        var secondRefreshResponse = await replayClient.PostAsync("/api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, secondRefreshResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WhenRateLimitExceeded_ReturnsTooManyRequests()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
            {
                RefreshToken = "invalid-refresh-token"
            }, JsonOptions);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var limitedResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        }, JsonOptions);

        Assert.Equal((HttpStatusCode)429, limitedResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithLegacyRequestBodyStillReturnsRotatedTokens()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = AvaEmail,
            Password = TravelPlannerApiFactory.SeedPassword
        }, JsonOptions);
        loginResponse.EnsureSuccessStatusCode();
        var issuedCookie = loginResponse.Headers.GetValues("Set-Cookie").Single();
        var refreshToken = issuedCookie
            .Split(';', 2)[0]
            .Split('=', 2)[1];

        using var legacyClient = factory.CreateApiClient();
        var response = await legacyClient.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        }, JsonOptions);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthSessionResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
    }

    [Fact]
    public async Task Logout_WithRefreshToken_RevokesRefreshToken()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = AvaEmail,
            Password = TravelPlannerApiFactory.SeedPassword
        }, JsonOptions);
        loginResponse.EnsureSuccessStatusCode();
        Assert.NotNull(await loginResponse.Content.ReadFromJsonAsync<AuthSessionResponse>(JsonOptions));

        var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await client.PostAsync("/api/auth/refresh", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_InvalidatesOldAccessAndRefreshTokensAndRequiresRelogin()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = AvaEmail,
            Password = TravelPlannerApiFactory.SeedPassword
        }, JsonOptions);
        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<AuthSessionResponse>(JsonOptions);
        Assert.NotNull(loginPayload);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginPayload!.AccessToken);

        var changePasswordResponse = await client.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest
        {
            CurrentPassword = TravelPlannerApiFactory.SeedPassword,
            NewPassword = "UpdatedPass123!",
            ConfirmNewPassword = "UpdatedPass123!"
        }, JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, changePasswordResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;

        var oldRefreshResponse = await client.PostAsync("/api/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, oldRefreshResponse.StatusCode);

        var oldLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = AvaEmail,
            Password = TravelPlannerApiFactory.SeedPassword
        }, JsonOptions);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);

        var newLoginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = AvaEmail,
            Password = "UpdatedPass123!"
        }, JsonOptions);
        newLoginResponse.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AuthMe_WithBearerToken_ReturnsCurrentUser()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var response = await client.GetAsync("/api/auth/me");

        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.NotNull(user);
        Assert.Equal("user-ava", user!.Id);
    }

    [Fact]
    public async Task Root_ReturnsSwaggerRedirect()
    {
        using var factory = new TravelPlannerApiFactory("Development");
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Root_OutsideDevelopment_ReturnsApiInfo()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("TravelPlannerApp API", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CspReportEndpoint_AcceptsAnonymousReports()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        using var content = new StringContent("{\"csp-report\":{\"violated-directive\":\"script-src\"}}");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/csp-report");

        var response = await client.PostAsync("/security/csp-reports", content);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsers_WithExplicitApiVersionHeader_ReturnsMatchesAndSupportedVersionHeader()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);
        client.DefaultRequestHeaders.Add("X-Api-Version", "1.0");

        var response = await client.GetAsync("/api/users?query=lu");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues("api-supported-versions", out var values));
        Assert.Contains("1.0", values!.Single());
        var payload = await response.Content.ReadFromJsonAsync<List<UserLookupResponse>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Contains(payload!, user => user.Id == "user-luca");
        Assert.DoesNotContain(payload!, user => user.Id == "user-ava");
    }

    [Fact]
    public async Task SwaggerJson_IncludesApiVersionIfMatchAndBearerSecurityScheme()
    {
        using var factory = new TravelPlannerApiFactory("Development");
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("X-Api-Version", payload, StringComparison.Ordinal);
        Assert.Contains("If-Match", payload, StringComparison.Ordinal);
        Assert.Contains("Bearer", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("X-User-Id", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SwaggerJson_OutsideDevelopment_ReturnsNotFound()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithUnsupportedApiVersion_ReturnsBadRequest()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);
        client.DefaultRequestHeaders.Add("X-Api-Version", "2.0");

        var response = await client.GetAsync("/api/users?query=lu");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsers_WithoutQuery_ReturnsValidationProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Contains("Query", payload!.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetItineraryById_WhenUserIsNotMember_ReturnsForbiddenProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(NoahEmail);

        var response = await client.GetAsync("/api/itineraries/itinerary-singapore");

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "Forbidden");
    }

    [Fact]
    public async Task CreateEvent_WithInvalidSchedule_ReturnsValidationProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var response = await client.PostAsJsonAsync("/api/itineraries/itinerary-tokyo/events", new CreateEventRequest
        {
            Title = "Broken",
            Category = EventCategory.Activity,
            StartDateTime = new DateTime(2026, 4, 15, 18, 0, 0),
            EndDateTime = new DateTime(2026, 4, 15, 18, 0, 0),
            Timezone = "Asia/Tokyo"
        }, JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Contains("endDateTime", payload!.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateEvent_WithValidPayload_CreatesEventAndAuditHistory()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var createResponse = await client.PostAsJsonAsync("/api/itineraries/itinerary-tokyo/events", new CreateEventRequest
        {
            Title = "Late Dinner",
            Category = EventCategory.Restaurant,
            StartDateTime = new DateTime(2026, 4, 16, 19, 0, 0),
            EndDateTime = new DateTime(2026, 4, 16, 21, 0, 0),
            Timezone = "Asia/Tokyo",
            Cost = 40m,
            CurrencyCode = "JPY"
        }, JsonOptions);

        createResponse.EnsureSuccessStatusCode();
        Assert.True(createResponse.Headers.ETag is not null);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);
        Assert.NotNull(createdEvent);
        Assert.Equal("JPY", createdEvent!.CurrencyCode);

        var history = await client.GetFromJsonAsync<List<EventAuditLogResponse>>($"/api/events/{createdEvent!.Id}/history", JsonOptions);

        Assert.NotNull(history);
        Assert.Single(history!);
        Assert.Equal(EventAuditAction.Created, history[0].Action);
    }

    [Fact]
    public async Task UpdateEvent_WithValidPayload_UpdatesEventAndAddsHistoryEntry()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var staleEtag = await GetEtagAsync(client, "/api/events/evt-tokyo-1");

        var updateResponse = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/events/evt-tokyo-1", new UpdateEventRequest
        {
            Title = "Shibuya Food Walk",
            Description = "Updated description",
            Category = EventCategory.Restaurant,
            Color = "#F97316",
            StartDateTime = new DateTime(2026, 4, 15, 18, 30, 0),
            EndDateTime = new DateTime(2026, 4, 15, 21, 45, 0),
            Timezone = "Asia/Tokyo",
            Location = "Shibuya",
            LocationAddress = "Dogenzaka, Shibuya City, Tokyo",
            LocationLat = 35.6595m,
            LocationLng = 139.7005m,
            Cost = 75m,
            CurrencyCode = "JPY"
        }, staleEtag));

        updateResponse.EnsureSuccessStatusCode();
        Assert.True(updateResponse.Headers.ETag is not null);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated description", updated!.Description);
        Assert.Equal("JPY", updated.CurrencyCode);
        Assert.NotEqual(TrimQuotes(staleEtag), updated.Version);

        var history = await client.GetFromJsonAsync<List<EventAuditLogResponse>>("/api/events/evt-tokyo-1/history", JsonOptions);

        Assert.NotNull(history);
        Assert.True(history!.Count >= 3);
        Assert.Equal(EventAuditAction.Updated, history[0].Action);
    }

    [Fact]
    public async Task UpdateEvent_WithoutIfMatch_ReturnsPreconditionRequiredProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var response = await client.PutAsJsonAsync("/api/events/evt-tokyo-1", new UpdateEventRequest
        {
            Title = "Shibuya Food Walk",
            Description = "Updated description",
            Category = EventCategory.Restaurant,
            Color = "#F97316",
            StartDateTime = new DateTime(2026, 4, 15, 18, 30, 0),
            EndDateTime = new DateTime(2026, 4, 15, 21, 45, 0),
            Timezone = "Asia/Tokyo",
            Location = "Shibuya",
            LocationAddress = "Dogenzaka, Shibuya City, Tokyo",
            LocationLat = 35.6595m,
            LocationLng = 139.7005m,
            Cost = 75m
        }, JsonOptions);

        await AssertProblemAsync(response, (HttpStatusCode)428, "Precondition Required");
    }

    [Fact]
    public async Task UpdateEvent_WithStaleIfMatch_ReturnsPreconditionFailedProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var staleEtag = await GetEtagAsync(client, "/api/events/evt-tokyo-1");

        var firstResponse = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/events/evt-tokyo-1", new UpdateEventRequest
        {
            Title = "Shibuya Food Walk",
            Description = "Fresh update",
            Category = EventCategory.Restaurant,
            Color = "#F97316",
            StartDateTime = new DateTime(2026, 4, 15, 18, 30, 0),
            EndDateTime = new DateTime(2026, 4, 15, 21, 45, 0),
            Timezone = "Asia/Tokyo",
            Location = "Shibuya",
            LocationAddress = "Dogenzaka, Shibuya City, Tokyo",
            LocationLat = 35.6595m,
            LocationLng = 139.7005m,
            Cost = 75m
        }, staleEtag));
        firstResponse.EnsureSuccessStatusCode();

        var staleResponse = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/events/evt-tokyo-1", new UpdateEventRequest
        {
            Title = "Shibuya Food Walk",
            Description = "Stale update",
            Category = EventCategory.Restaurant,
            Color = "#F97316",
            StartDateTime = new DateTime(2026, 4, 15, 18, 30, 0),
            EndDateTime = new DateTime(2026, 4, 15, 21, 45, 0),
            Timezone = "Asia/Tokyo",
            Location = "Shibuya",
            LocationAddress = "Dogenzaka, Shibuya City, Tokyo",
            LocationLat = 35.6595m,
            LocationLng = 139.7005m,
            Cost = 75m
        }, staleEtag));

        await AssertProblemAsync(staleResponse, HttpStatusCode.PreconditionFailed, "Precondition Failed");
    }

    [Fact]
    public async Task CreateEvent_WhenUserIsItineraryMember_ReturnsCreated()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);

        var response = await client.PostAsJsonAsync("/api/itineraries/itinerary-tokyo/events", new CreateEventRequest
        {
            Title = "Late-night ramen stop",
            Description = "Member-created event",
            Category = EventCategory.Restaurant,
            Color = "#F97316",
            StartDateTime = new DateTime(2026, 4, 16, 22, 0, 0),
            EndDateTime = new DateTime(2026, 4, 16, 23, 0, 0),
            Timezone = "Asia/Tokyo",
            Location = "Shibuya",
            LocationAddress = "Dogenzaka, Shibuya City, Tokyo"
        }, JsonOptions);

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Late-night ramen stop", payload!.Title);
    }

    [Fact]
    public async Task UpdateEvent_WhenUserIsItineraryMember_ReturnsOk()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);
        var staleEtag = await GetEtagAsync(client, "/api/events/evt-tokyo-1");

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/events/evt-tokyo-1", new UpdateEventRequest
        {
            Title = "Member update",
            Description = "Updated by itinerary member",
            Category = EventCategory.Restaurant,
            Color = "#F97316",
            StartDateTime = new DateTime(2026, 4, 15, 18, 30, 0),
            EndDateTime = new DateTime(2026, 4, 15, 21, 45, 0),
            Timezone = "Asia/Tokyo",
            Location = "Shibuya",
            LocationAddress = "Dogenzaka, Shibuya City, Tokyo"
        }, staleEtag));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Member update", payload!.Title);
        Assert.Equal("user-luca", payload.UpdatedById);
    }

    [Fact]
    public async Task UpdateEvent_WhenUserIsNotMember_ReturnsForbidden()
    {
        using var factory = new TravelPlannerApiFactory();
        using var ownerClient = await factory.CreateAuthenticatedClientAsync(AvaEmail);
        var staleEtag = await GetEtagAsync(ownerClient, "/api/events/evt-tokyo-1");
        using var client = await factory.CreateAuthenticatedClientAsync(NoahEmail);

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/events/evt-tokyo-1", new UpdateEventRequest
        {
            Title = "Blocked update",
            Description = "Blocked update",
            Category = EventCategory.Restaurant,
            Color = "#F97316",
            StartDateTime = new DateTime(2026, 4, 15, 18, 30, 0),
            EndDateTime = new DateTime(2026, 4, 15, 21, 45, 0),
            Timezone = "Asia/Tokyo"
        }, staleEtag));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WhenItineraryOwnerDeletesMemberEvent_LeavesAuditHistoryAccessible()
    {
        using var factory = new TravelPlannerApiFactory();
        using var creatorClient = await factory.CreateAuthenticatedClientAsync(AvaEmail);
        using var memberClient = await factory.CreateAuthenticatedClientAsync(LucaEmail);

        var createResponse = await memberClient.PostAsJsonAsync("/api/itineraries/itinerary-tokyo/events", new CreateEventRequest
        {
            Title = "Delete Me",
            Category = EventCategory.Other,
            StartDateTime = new DateTime(2026, 4, 17, 10, 0, 0),
            EndDateTime = new DateTime(2026, 4, 17, 11, 0, 0),
            Timezone = "Asia/Tokyo"
        }, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);

        var deleteResponse = await creatorClient.SendAsync(CreateIfMatchRequest(HttpMethod.Delete, $"/api/events/{created!.Id}", null, createResponse.Headers.ETag!.Tag!));
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var history = await memberClient.GetFromJsonAsync<List<EventAuditLogResponse>>($"/api/events/{created.Id}/history", JsonOptions);

        Assert.NotNull(history);
        Assert.Equal(2, history!.Count);
        Assert.Contains(history, log => log.Action == EventAuditAction.Deleted);
    }

    [Fact]
    public async Task DeleteEvent_WhenMemberDeletesOwnEvent_ReturnsNoContent()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);

        var createResponse = await client.PostAsJsonAsync("/api/itineraries/itinerary-tokyo/events", new CreateEventRequest
        {
            Title = "Delete My Own Event",
            Category = EventCategory.Other,
            StartDateTime = new DateTime(2026, 4, 17, 13, 0, 0),
            EndDateTime = new DateTime(2026, 4, 17, 14, 0, 0),
            Timezone = "Asia/Tokyo"
        }, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);

        var deleteResponse = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Delete, $"/api/events/{created!.Id}", null, createResponse.Headers.ETag!.Tag!));

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WhenMemberDeletesAnotherUsersEvent_ReturnsForbidden()
    {
        using var factory = new TravelPlannerApiFactory();
        using var ownerClient = await factory.CreateAuthenticatedClientAsync(AvaEmail);
        using var memberClient = await factory.CreateAuthenticatedClientAsync(LucaEmail);
        var staleEtag = await GetEtagAsync(ownerClient, "/api/events/evt-tokyo-1");

        var response = await memberClient.SendAsync(CreateIfMatchRequest(HttpMethod.Delete, "/api/events/evt-tokyo-1", null, staleEtag));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAccessibleItineraries_WithValidUser_ReturnsSeededMemberships()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);

        var itineraries = await client.GetFromJsonAsync<List<ItineraryResponse>>("/api/itineraries", JsonOptions);

        Assert.NotNull(itineraries);
        Assert.Contains(itineraries!, itinerary => itinerary.Id == "itinerary-tokyo");
        Assert.Contains(itineraries!, itinerary => itinerary.Id == "itinerary-singapore");
    }

    [Fact]
    public async Task UpdateItinerary_WhenUserIsNotOwner_ReturnsForbidden()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);
        var staleEtag = await GetEtagAsync(client, "/api/itineraries/itinerary-tokyo");

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/itineraries/itinerary-tokyo", new UpdateItineraryRequest
        {
            Title = "Blocked",
            Destination = "Tokyo",
            Description = "Blocked",
            StartDate = new DateOnly(2026, 4, 14),
            EndDate = new DateOnly(2026, 4, 18)
        }, staleEtag));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReplaceMembers_WhenCreatorIsOmitted_KeepsCreatorInResponse()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var staleEtag = await GetEtagAsync(client, "/api/itineraries/itinerary-tokyo/members");

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/itineraries/itinerary-tokyo/members", new ReplaceItineraryMembersRequest
        {
            UserIds = ["user-luca"]
        }, staleEtag));

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.ETag is not null);
        var members = await response.Content.ReadFromJsonAsync<List<ItineraryMemberResponse>>(JsonOptions);

        Assert.NotNull(members);
        Assert.Contains(members!, member => member.UserId == "user-ava");
    }

    [Fact]
    public async Task ReplaceMembers_WhenUserIsNotOwner_ReturnsForbidden()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);
        var staleEtag = await GetEtagAsync(client, "/api/itineraries/itinerary-tokyo/members");

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/itineraries/itinerary-tokyo/members", new ReplaceItineraryMembersRequest
        {
            UserIds = ["user-luca", "user-mina"]
        }, staleEtag));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_WhenOwnerRemovesContributor_ReturnsUpdatedMembers()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);
        var staleEtag = await GetEtagAsync(client, "/api/itineraries/itinerary-tokyo/members");

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Delete, "/api/itineraries/itinerary-tokyo/members/user-mina", null, staleEtag));

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.ETag is not null);
        var members = await response.Content.ReadFromJsonAsync<List<ItineraryMemberResponse>>(JsonOptions);

        Assert.NotNull(members);
        Assert.DoesNotContain(members!, member => member.UserId == "user-mina");
        Assert.Contains(members!, member => member.UserId == "user-ava");
    }

    [Fact]
    public async Task RemoveMember_WhenUserIsNotOwner_ReturnsForbidden()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);
        var staleEtag = await GetEtagAsync(client, "/api/itineraries/itinerary-tokyo/members");

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Delete, "/api/itineraries/itinerary-tokyo/members/user-mina", null, staleEtag));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_WhenTargetingOwner_ReturnsBadRequestProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);
        var staleEtag = await GetEtagAsync(client, "/api/itineraries/itinerary-tokyo/members");

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Delete, "/api/itineraries/itinerary-tokyo/members/user-ava", null, staleEtag));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Bad Request");
    }

    [Fact]
    public async Task GetEventById_ReturnsEtagHeader()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var response = await client.GetAsync("/api/events/evt-tokyo-1");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.ETag is not null);
    }

    [Fact]
    public async Task GetEventById_WhenUserIsMemberButNotOwner_ReturnsEvent()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);

        var response = await client.GetAsync("/api/events/evt-tokyo-1");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal("evt-tokyo-1", payload!.Id);
    }

    [Fact]
    public async Task GetEventHistory_WhenUserIsNotMember_ReturnsForbiddenProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(NoahEmail);

        var response = await client.GetAsync("/api/events/evt-tokyo-1/history");

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "Forbidden");
    }

    [Fact]
    public async Task CreateUser_WithExistingEmail_ReturnsConflictProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            Name = "Duplicate",
            Email = AvaEmail,
            Password = "Pass12345!"
        }, JsonOptions);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Conflict");
    }

    [Fact]
    public async Task CreateUser_ResponseContainsVersionAndEtag()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            Name = "New User",
            Email = "new.user@example.com",
            Password = "Pass12345!"
        }, JsonOptions);

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.ETag is not null);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.NotNull(user);
        Assert.False(string.IsNullOrWhiteSpace(user!.Version));
    }

    [Fact]
    public async Task GetUser_WhenTargetingOwnProfile_ReturnsUser()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(AvaEmail);

        var response = await client.GetAsync("/api/users/user-ava");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.ETag is not null);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.NotNull(user);
        Assert.Equal("user-ava", user!.Id);
    }

    [Fact]
    public async Task GetUser_WhenTargetingAnotherProfile_ReturnsForbidden()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);

        var response = await client.GetAsync("/api/users/user-ava");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WhenTargetingAnotherProfile_ReturnsForbidden()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = await factory.CreateAuthenticatedClientAsync(LucaEmail);

        var response = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/users/user-ava", new UpdateUserRequest
        {
            Name = "Blocked",
            Email = AvaEmail,
            Avatar = "AS"
        }, "\"blocked-version\""));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpRequestMessage CreateIfMatchRequest(HttpMethod method, string path, object? body, string etag)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return request;
    }

    private static async Task<string> GetEtagAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var etag = response.Headers.ETag?.Tag;
        Assert.False(string.IsNullOrWhiteSpace(etag));
        return etag!;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode statusCode, string title)
    {
        Assert.Equal(statusCode, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(title, problem!.Title);
    }

    private static string TrimQuotes(string value)
    {
        return value.Trim().Trim('"');
    }

    private sealed record ProblemResponse(string Title, int? Status, string? Detail);

    private sealed class ValidationProblemResponse
    {
        public Dictionary<string, string[]> Errors { get; set; } = [];
    }
}
