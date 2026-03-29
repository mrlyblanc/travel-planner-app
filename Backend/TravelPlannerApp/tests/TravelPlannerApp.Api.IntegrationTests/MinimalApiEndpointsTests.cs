using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TravelPlannerApp.Api.IntegrationTests.Support;
using TravelPlannerApp.Application.Contracts.Events;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Api.IntegrationTests;

public sealed class MinimalApiEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task GetUsers_ReturnsSeededUsers()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var users = await client.GetFromJsonAsync<List<UserResponse>>("/api/users", JsonOptions);

        Assert.NotNull(users);
        Assert.Contains(users!, user => user.Id == "user-ava" && user.Email == "ava.santos@globejet.com");
        Assert.All(users!, user => Assert.False(string.IsNullOrWhiteSpace(user.Version)));
    }

    [Fact]
    public async Task Root_ReturnsSwaggerRedirect()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task GetUsers_WithExplicitApiVersionHeader_ReturnsUsersAndSupportedVersionHeader()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-Api-Version", "1.0");

        var response = await client.GetAsync("/api/users");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues("api-supported-versions", out var values));
        Assert.Contains("1.0", values!.Single());
    }

    [Fact]
    public async Task SwaggerJson_IncludesApiVersionCurrentUserAndIfMatchHeaders()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("X-Api-Version", payload, StringComparison.Ordinal);
        Assert.Contains("X-User-Id", payload, StringComparison.Ordinal);
        Assert.Contains("If-Match", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUsers_WithUnsupportedApiVersion_ReturnsBadRequest()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-Api-Version", "2.0");

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetItineraries_WithoutCurrentUserHeader_ReturnsBadRequestProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.GetAsync("/api/itineraries");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Bad Request");
    }

    [Fact]
    public async Task GetItineraries_WithUnknownCurrentUser_ReturnsUnauthorizedProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-missing");

        var response = await client.GetAsync("/api/itineraries");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Fact]
    public async Task GetItineraryById_WhenCurrentUserIsNotAMember_ReturnsForbiddenProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-mina");

        var response = await client.GetAsync("/api/itineraries/itinerary-singapore");

        await AssertProblemAsync(response, HttpStatusCode.Forbidden, "Forbidden");
    }

    [Fact]
    public async Task CreateEvent_WithInvalidSchedule_ReturnsValidationProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

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
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

        var createResponse = await client.PostAsJsonAsync("/api/itineraries/itinerary-tokyo/events", new CreateEventRequest
        {
            Title = "Late Dinner",
            Category = EventCategory.Restaurant,
            StartDateTime = new DateTime(2026, 4, 16, 19, 0, 0),
            EndDateTime = new DateTime(2026, 4, 16, 21, 0, 0),
            Timezone = "Asia/Tokyo",
            Cost = 40m
        }, JsonOptions);

        createResponse.EnsureSuccessStatusCode();
        Assert.True(createResponse.Headers.ETag is not null);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);
        Assert.NotNull(createdEvent);
        Assert.False(string.IsNullOrWhiteSpace(createdEvent!.Version));

        var history = await client.GetFromJsonAsync<List<EventAuditLogResponse>>($"/api/events/{createdEvent.Id}/history", JsonOptions);

        Assert.NotNull(history);
        Assert.Single(history!);
        Assert.Equal(EventAuditAction.Created, history[0].Action);
    }

    [Fact]
    public async Task UpdateEvent_WithValidPayload_UpdatesEventAndAddsHistoryEntry()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

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
            Cost = 75m
        }, staleEtag));

        updateResponse.EnsureSuccessStatusCode();
        Assert.True(updateResponse.Headers.ETag is not null);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated description", updated!.Description);
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
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

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
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

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
    public async Task DeleteEvent_AfterCreation_LeavesAuditHistoryAccessible()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

        var createResponse = await client.PostAsJsonAsync("/api/itineraries/itinerary-tokyo/events", new CreateEventRequest
        {
            Title = "Delete Me",
            Category = EventCategory.Other,
            StartDateTime = new DateTime(2026, 4, 17, 10, 0, 0),
            EndDateTime = new DateTime(2026, 4, 17, 11, 0, 0),
            Timezone = "Asia/Tokyo"
        }, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);

        var deleteResponse = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Delete, $"/api/events/{created!.Id}", null, createResponse.Headers.ETag!.Tag!));
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var history = await client.GetFromJsonAsync<List<EventAuditLogResponse>>($"/api/events/{created.Id}/history", JsonOptions);

        Assert.NotNull(history);
        Assert.Equal(2, history!.Count);
        Assert.Contains(history, log => log.Action == EventAuditAction.Deleted);
    }

    [Fact]
    public async Task GetAccessibleItineraries_WithValidCurrentUser_ReturnsSeededMemberships()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-luca");

        var itineraries = await client.GetFromJsonAsync<List<ItineraryResponse>>("/api/itineraries", JsonOptions);

        Assert.NotNull(itineraries);
        Assert.Contains(itineraries!, itinerary => itinerary.Id == "itinerary-tokyo");
        Assert.Contains(itineraries!, itinerary => itinerary.Id == "itinerary-singapore");
        Assert.All(itineraries!, itinerary => Assert.False(string.IsNullOrWhiteSpace(itinerary.Version)));
    }

    [Fact]
    public async Task ReplaceMembers_WhenCreatorIsOmitted_KeepsCreatorInResponse()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

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
    public async Task ReplaceMembers_WithStaleIfMatch_ReturnsPreconditionFailedProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

        var staleEtag = await GetEtagAsync(client, "/api/itineraries/itinerary-tokyo/members");

        var firstResponse = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/itineraries/itinerary-tokyo/members", new ReplaceItineraryMembersRequest
        {
            UserIds = ["user-luca", "user-mina"]
        }, staleEtag));
        firstResponse.EnsureSuccessStatusCode();

        var staleResponse = await client.SendAsync(CreateIfMatchRequest(HttpMethod.Put, "/api/itineraries/itinerary-tokyo/members", new ReplaceItineraryMembersRequest
        {
            UserIds = ["user-luca"]
        }, staleEtag));

        await AssertProblemAsync(staleResponse, HttpStatusCode.PreconditionFailed, "Precondition Failed");
    }

    [Fact]
    public async Task GetEventById_ReturnsEtagHeader()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

        var response = await client.GetAsync("/api/events/evt-tokyo-1");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.ETag is not null);
    }

    [Fact]
    public async Task GetItineraryMembers_ReturnsItineraryEtagHeader()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

        var response = await client.GetAsync("/api/itineraries/itinerary-tokyo/members");

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.ETag is not null);
    }

    [Fact]
    public async Task CreateUser_WithExistingEmail_ReturnsConflictProblem()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest
        {
            Name = "Duplicate",
            Email = "ava.santos@globejet.com"
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
            Email = "new.user@example.com"
        }, JsonOptions);

        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.ETag is not null);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.NotNull(user);
        Assert.False(string.IsNullOrWhiteSpace(user!.Version));
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
