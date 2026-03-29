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
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);
        Assert.NotNull(createdEvent);

        var history = await client.GetFromJsonAsync<List<EventAuditLogResponse>>($"/api/events/{createdEvent!.Id}/history", JsonOptions);

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

        var updateResponse = await client.PutAsJsonAsync("/api/events/evt-tokyo-1", new UpdateEventRequest
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

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Updated description", updated!.Description);

        var history = await client.GetFromJsonAsync<List<EventAuditLogResponse>>("/api/events/evt-tokyo-1/history", JsonOptions);

        Assert.NotNull(history);
        Assert.True(history!.Count >= 3);
        Assert.Equal(EventAuditAction.Updated, history[0].Action);
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
        var created = await createResponse.Content.ReadFromJsonAsync<EventResponse>(JsonOptions);

        var deleteResponse = await client.DeleteAsync($"/api/events/{created!.Id}");
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
    }

    [Fact]
    public async Task ReplaceMembers_WhenCreatorIsOmitted_KeepsCreatorInResponse()
    {
        using var factory = new TravelPlannerApiFactory();
        using var client = factory.CreateApiClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "user-ava");

        var response = await client.PutAsJsonAsync("/api/itineraries/itinerary-tokyo/members", new ReplaceItineraryMembersRequest
        {
            UserIds = ["user-luca"]
        }, JsonOptions);

        response.EnsureSuccessStatusCode();
        var members = await response.Content.ReadFromJsonAsync<List<ItineraryMemberResponse>>(JsonOptions);

        Assert.NotNull(members);
        Assert.Contains(members!, member => member.UserId == "user-ava");
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

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode statusCode, string title)
    {
        Assert.Equal(statusCode, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>(JsonOptions);
        Assert.NotNull(problem);
        Assert.Equal(title, problem!.Title);
    }

    private sealed record ProblemResponse(string Title, int? Status, string? Detail);

    private sealed class ValidationProblemResponse
    {
        public Dictionary<string, string[]> Errors { get; set; } = [];
    }
}
