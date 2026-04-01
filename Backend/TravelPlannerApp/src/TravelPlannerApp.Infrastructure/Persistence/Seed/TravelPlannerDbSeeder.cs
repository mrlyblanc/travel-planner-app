using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TravelPlannerApp.Application.Abstractions.Security;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Mappings;
using TravelPlannerApp.Domain.Entities;
using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Infrastructure.Persistence.Seed;

public sealed class TravelPlannerDbSeeder
{
    private readonly TravelPlannerDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TravelPlannerDbSeeder> _logger;
    private readonly IPasswordHasher _passwordHasher;

    public TravelPlannerDbSeeder(
        TravelPlannerDbContext dbContext,
        IConfiguration configuration,
        ILogger<TravelPlannerDbSeeder> logger,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled(_configuration["Seed:Enabled"]))
        {
            _logger.LogInformation("Seed data is disabled for this environment");
            return;
        }

        if (await _dbContext.Users.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("Seed data skipped because users already exist");
            return;
        }

        _logger.LogInformation("Seeding initial travel planner data");

        var users = CreateUsers(GetSeedPassword());
        await _dbContext.Users.AddRangeAsync(users, cancellationToken);

        var itineraries = CreateItineraries();
        await _dbContext.Itineraries.AddRangeAsync(itineraries, cancellationToken);

        var members = CreateMembers();
        await _dbContext.ItineraryMembers.AddRangeAsync(members, cancellationToken);

        var events = CreateEvents();
        await _dbContext.Events.AddRangeAsync(events, cancellationToken);

        var auditLogs = CreateAuditLogs(events);
        await _dbContext.EventAuditLogs.AddRangeAsync(auditLogs, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seed data inserted successfully");
    }

    private List<User> CreateUsers(string seedPassword)
    {
        var createdAt = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc);
        var passwordHash = _passwordHasher.HashPassword(seedPassword);

        return
        [
            new User { Id = "user-ava", ConcurrencyToken = "00000000000000000000000000000011", AuthVersion = "30000000000000000000000000000011", Name = "Ava Santos", Email = "ava.santos@globejet.com", PasswordHash = passwordHash, Avatar = "AS", CreatedAtUtc = createdAt },
            new User { Id = "user-luca", ConcurrencyToken = "00000000000000000000000000000012", AuthVersion = "30000000000000000000000000000012", Name = "Luca Reyes", Email = "luca.reyes@globejet.com", PasswordHash = passwordHash, Avatar = "LR", CreatedAtUtc = createdAt.AddMinutes(2) },
            new User { Id = "user-mina", ConcurrencyToken = "00000000000000000000000000000013", AuthVersion = "30000000000000000000000000000013", Name = "Mina Park", Email = "mina.park@globejet.com", PasswordHash = passwordHash, Avatar = "MP", CreatedAtUtc = createdAt.AddMinutes(4) },
            new User { Id = "user-ethan", ConcurrencyToken = "00000000000000000000000000000014", AuthVersion = "30000000000000000000000000000014", Name = "Ethan Cruz", Email = "ethan.cruz@globejet.com", PasswordHash = passwordHash, Avatar = "EC", CreatedAtUtc = createdAt.AddMinutes(6) },
            new User { Id = "user-sofia", ConcurrencyToken = "00000000000000000000000000000015", AuthVersion = "30000000000000000000000000000015", Name = "Sofia Lim", Email = "sofia.lim@globejet.com", PasswordHash = passwordHash, Avatar = "SL", CreatedAtUtc = createdAt.AddMinutes(8) },
            new User { Id = "user-noah", ConcurrencyToken = "00000000000000000000000000000016", AuthVersion = "30000000000000000000000000000016", Name = "Noah Tan", Email = "noah.tan@globejet.com", PasswordHash = passwordHash, Avatar = "NT", CreatedAtUtc = createdAt.AddMinutes(10) }
        ];
    }

    private string GetSeedPassword()
    {
        var password = _configuration["Seed:DefaultUserPassword"]?.Trim();
        if (string.IsNullOrWhiteSpace(password) || string.Equals(password, "__set_in_env__", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Seed:DefaultUserPassword must be configured before seeding users.");
        }

        return password;
    }

    private static bool IsEnabled(string? rawValue)
    {
        return bool.TryParse(rawValue, out var enabled) && enabled;
    }

    private static List<Itinerary> CreateItineraries()
    {
        var createdAt = new DateTime(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);

        return
        [
            new Itinerary
            {
                Id = "itinerary-tokyo",
                ConcurrencyToken = "10000000000000000000000000000011",
                Title = "Tokyo Food Sprint",
                Description = "A packed four-day food and district itinerary.",
                Destination = "Tokyo, Japan",
                ShareCode = "48152",
                ShareCodeUpdatedAtUtc = createdAt,
                StartDate = new DateOnly(2026, 4, 14),
                EndDate = new DateOnly(2026, 4, 18),
                CreatedById = "user-ava",
                CreatedAtUtc = createdAt,
                UpdatedAtUtc = createdAt
            },
            new Itinerary
            {
                Id = "itinerary-seoul",
                ConcurrencyToken = "10000000000000000000000000000012",
                Title = "Seoul Design Week",
                Description = "Cafes, design shops, and gallery stops.",
                Destination = "Seoul, South Korea",
                ShareCode = "26471",
                ShareCodeUpdatedAtUtc = createdAt.AddDays(1),
                StartDate = new DateOnly(2026, 5, 4),
                EndDate = new DateOnly(2026, 5, 8),
                CreatedById = "user-luca",
                CreatedAtUtc = createdAt.AddDays(1),
                UpdatedAtUtc = createdAt.AddDays(1)
            },
            new Itinerary
            {
                Id = "itinerary-singapore",
                ConcurrencyToken = "10000000000000000000000000000013",
                Title = "Singapore Weekend Reset",
                Description = "Fast city weekend with hawker centers and gardens.",
                Destination = "Singapore",
                ShareCode = "39184",
                ShareCodeUpdatedAtUtc = createdAt.AddDays(2),
                StartDate = new DateOnly(2026, 6, 12),
                EndDate = new DateOnly(2026, 6, 15),
                CreatedById = "user-ava",
                CreatedAtUtc = createdAt.AddDays(2),
                UpdatedAtUtc = createdAt.AddDays(2)
            },
            new Itinerary
            {
                Id = "itinerary-bali",
                ConcurrencyToken = "10000000000000000000000000000014",
                Title = "Bali Surf Days",
                Description = "Beach, surf lessons, and sunset dinners.",
                Destination = "Bali, Indonesia",
                ShareCode = "57206",
                ShareCodeUpdatedAtUtc = createdAt.AddDays(3),
                StartDate = new DateOnly(2026, 7, 6),
                EndDate = new DateOnly(2026, 7, 11),
                CreatedById = "user-sofia",
                CreatedAtUtc = createdAt.AddDays(3),
                UpdatedAtUtc = createdAt.AddDays(3)
            },
            new Itinerary
            {
                Id = "itinerary-manila",
                ConcurrencyToken = "10000000000000000000000000000015",
                Title = "Manila Family Visit",
                Description = "Flexible city schedule with family stops.",
                Destination = "Manila, Philippines",
                ShareCode = "84513",
                ShareCodeUpdatedAtUtc = createdAt.AddDays(4),
                StartDate = new DateOnly(2026, 8, 20),
                EndDate = new DateOnly(2026, 8, 24),
                CreatedById = "user-noah",
                CreatedAtUtc = createdAt.AddDays(4),
                UpdatedAtUtc = createdAt.AddDays(4)
            }
        ];
    }

    private static List<ItineraryMember> CreateMembers()
    {
        var addedAt = new DateTime(2026, 2, 1, 9, 30, 0, DateTimeKind.Utc);

        return
        [
            new ItineraryMember { ItineraryId = "itinerary-tokyo", UserId = "user-ava", AddedByUserId = "user-ava", AddedAtUtc = addedAt },
            new ItineraryMember { ItineraryId = "itinerary-tokyo", UserId = "user-luca", AddedByUserId = "user-ava", AddedAtUtc = addedAt.AddMinutes(1) },
            new ItineraryMember { ItineraryId = "itinerary-tokyo", UserId = "user-mina", AddedByUserId = "user-ava", AddedAtUtc = addedAt.AddMinutes(2) },
            new ItineraryMember { ItineraryId = "itinerary-seoul", UserId = "user-luca", AddedByUserId = "user-luca", AddedAtUtc = addedAt.AddDays(1) },
            new ItineraryMember { ItineraryId = "itinerary-seoul", UserId = "user-sofia", AddedByUserId = "user-luca", AddedAtUtc = addedAt.AddDays(1).AddMinutes(1) },
            new ItineraryMember { ItineraryId = "itinerary-singapore", UserId = "user-ava", AddedByUserId = "user-ava", AddedAtUtc = addedAt.AddDays(2) },
            new ItineraryMember { ItineraryId = "itinerary-singapore", UserId = "user-luca", AddedByUserId = "user-ava", AddedAtUtc = addedAt.AddDays(2).AddMinutes(1) },
            new ItineraryMember { ItineraryId = "itinerary-singapore", UserId = "user-ethan", AddedByUserId = "user-ava", AddedAtUtc = addedAt.AddDays(2).AddMinutes(2) },
            new ItineraryMember { ItineraryId = "itinerary-bali", UserId = "user-sofia", AddedByUserId = "user-sofia", AddedAtUtc = addedAt.AddDays(3) },
            new ItineraryMember { ItineraryId = "itinerary-bali", UserId = "user-noah", AddedByUserId = "user-sofia", AddedAtUtc = addedAt.AddDays(3).AddMinutes(1) },
            new ItineraryMember { ItineraryId = "itinerary-manila", UserId = "user-noah", AddedByUserId = "user-noah", AddedAtUtc = addedAt.AddDays(4) },
            new ItineraryMember { ItineraryId = "itinerary-manila", UserId = "user-ava", AddedByUserId = "user-noah", AddedAtUtc = addedAt.AddDays(4).AddMinutes(1) }
        ];
    }

    private static List<Event> CreateEvents()
    {
        var createdAt = new DateTime(2026, 2, 10, 11, 0, 0, DateTimeKind.Utc);

        return
        [
            new Event
            {
                Id = "evt-tokyo-1",
                ConcurrencyToken = "20000000000000000000000000000011",
                ItineraryId = "itinerary-tokyo",
                Title = "Shibuya Food Walk",
                Description = "Izakaya crawl and late ramen stop.",
                Remarks = "Aim for the first reservation slot so the crossing is still lively after dinner.",
                Category = EventCategory.Restaurant,
                Color = "#F97316",
                IsAllDay = false,
                StartDateTimeLocal = new DateTime(2026, 4, 15, 18, 0, 0, DateTimeKind.Unspecified),
                EndDateTimeLocal = new DateTime(2026, 4, 15, 21, 30, 0, DateTimeKind.Unspecified),
                Timezone = "Asia/Tokyo",
                Location = "Shibuya",
                LocationAddress = "Dogenzaka, Shibuya City, Tokyo",
                LocationLat = 35.659500m,
                LocationLng = 139.700500m,
                Cost = 65m,
                CurrencyCode = "JPY",
                CreatedById = "user-ava",
                UpdatedById = "user-luca",
                CreatedAtUtc = createdAt,
                UpdatedAtUtc = createdAt.AddHours(2),
                Links =
                [
                    new EventLink { Id = "lnk-tokyo-1", Description = "Reservation page", Url = "https://example.com/shibuya-food-walk", SortOrder = 0 },
                    new EventLink { Id = "lnk-tokyo-2", Description = "Meeting point map", Url = "https://maps.example.com/shibuya-food-walk", SortOrder = 1 }
                ]
            },
            new Event
            {
                Id = "evt-seoul-1",
                ConcurrencyToken = "20000000000000000000000000000012",
                ItineraryId = "itinerary-seoul",
                Title = "DDP Night Visit",
                Description = "Evening architecture walk and exhibit pass.",
                Remarks = "Buy the exhibit pass online before noon to avoid the evening queue.",
                Category = EventCategory.Landmark,
                Color = "#0F766E",
                IsAllDay = false,
                StartDateTimeLocal = new DateTime(2026, 5, 5, 19, 0, 0, DateTimeKind.Unspecified),
                EndDateTimeLocal = new DateTime(2026, 5, 5, 21, 0, 0, DateTimeKind.Unspecified),
                Timezone = "Asia/Seoul",
                Location = "Dongdaemun Design Plaza",
                LocationAddress = "281 Eulji-ro, Jung District, Seoul",
                LocationLat = 37.566500m,
                LocationLng = 127.009200m,
                Cost = 24m,
                CurrencyCode = "KRW",
                CreatedById = "user-luca",
                UpdatedById = "user-luca",
                CreatedAtUtc = createdAt.AddDays(1),
                UpdatedAtUtc = createdAt.AddDays(1),
                Links =
                [
                    new EventLink { Id = "lnk-seoul-1", Description = "Exhibit tickets", Url = "https://example.com/ddp-tickets", SortOrder = 0 }
                ]
            },
            new Event
            {
                Id = "evt-singapore-1",
                ConcurrencyToken = "20000000000000000000000000000013",
                ItineraryId = "itinerary-singapore",
                Title = "Gardens by the Bay",
                Description = "Cloud Forest in the afternoon.",
                Remarks = "Keep a light jacket for the cooled conservatory.",
                Category = EventCategory.Activity,
                Color = "#16A34A",
                IsAllDay = false,
                StartDateTimeLocal = new DateTime(2026, 6, 13, 14, 0, 0, DateTimeKind.Unspecified),
                EndDateTimeLocal = new DateTime(2026, 6, 13, 17, 0, 0, DateTimeKind.Unspecified),
                Timezone = "Asia/Singapore",
                Location = "Gardens by the Bay",
                LocationAddress = "18 Marina Gardens Dr, Singapore",
                LocationLat = 1.281600m,
                LocationLng = 103.863600m,
                Cost = 32m,
                CurrencyCode = "SGD",
                CreatedById = "user-ava",
                UpdatedById = "user-ava",
                CreatedAtUtc = createdAt.AddDays(2),
                UpdatedAtUtc = createdAt.AddDays(2),
                Links =
                [
                    new EventLink { Id = "lnk-singapore-1", Description = "Attraction details", Url = "https://example.com/gardens-by-the-bay", SortOrder = 0 }
                ]
            }
        ];
    }

    private static List<EventAuditLog> CreateAuditLogs(List<Event> events)
    {
        var eventLookup = events.ToDictionary(static eventEntity => eventEntity.Id, StringComparer.OrdinalIgnoreCase);

        return
        [
            CreateAuditLog(eventLookup["evt-tokyo-1"], "audit-tokyo-created", EventAuditAction.Created, "Created event 'Shibuya Food Walk'.", "user-ava", new DateTime(2026, 2, 10, 11, 0, 0, DateTimeKind.Utc)),
            CreateAuditLog(eventLookup["evt-tokyo-1"], "audit-tokyo-updated", EventAuditAction.Updated, "Updated event 'Shibuya Food Walk'.", "user-luca", new DateTime(2026, 2, 10, 13, 0, 0, DateTimeKind.Utc)),
            CreateAuditLog(eventLookup["evt-seoul-1"], "audit-seoul-created", EventAuditAction.Created, "Created event 'DDP Night Visit'.", "user-luca", new DateTime(2026, 2, 11, 11, 0, 0, DateTimeKind.Utc)),
            CreateAuditLog(eventLookup["evt-singapore-1"], "audit-singapore-created", EventAuditAction.Created, "Created event 'Gardens by the Bay'.", "user-ava", new DateTime(2026, 2, 12, 11, 0, 0, DateTimeKind.Utc))
        ];
    }

    private static EventAuditLog CreateAuditLog(Event eventEntity, string auditId, EventAuditAction action, string summary, string changedByUserId, DateTime changedAtUtc)
    {
        return new EventAuditLog
        {
            Id = auditId,
            EventId = eventEntity.Id,
            ItineraryId = eventEntity.ItineraryId,
            Action = action,
            Summary = summary,
            SnapshotJson = JsonSerializer.Serialize(eventEntity.ToAuditSnapshot(), AuditJson.Options),
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = changedAtUtc
        };
    }
}
