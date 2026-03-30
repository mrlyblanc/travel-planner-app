using System.Text.Json;
using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Application.Abstractions.Realtime;
using TravelPlannerApp.Application.Abstractions.Security;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Mappings;
using TravelPlannerApp.Domain.Entities;
using TravelPlannerApp.Domain.Enums;

namespace TravelPlannerApp.Application.Tests.Support;

internal sealed class FakeCurrentUserAccessor : ICurrentUserAccessor
{
    public string? CurrentUserId { get; set; }

    public string? GetCurrentUserId() => CurrentUserId;
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalls++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeRealtimeNotifier : IItineraryRealtimeNotifier
{
    public List<ItineraryRealtimeNotification> Notifications { get; } = [];

    public Task NotifyAsync(string itineraryId, ItineraryRealtimeNotification notification, CancellationToken cancellationToken = default)
    {
        Notifications.Add(notification);
        return Task.CompletedTask;
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return $"hashed::{password}";
    }

    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        return string.Equals(hashedPassword, HashPassword(providedPassword), StringComparison.Ordinal);
    }
}

internal sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public TokenResult GenerateToken(User user)
    {
        return new TokenResult($"token-for-{user.Id}-{user.AuthVersion}", new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
    }
}

internal sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
{
    private int _counter;

    public string GenerateToken()
    {
        _counter++;
        return $"refresh-token-{_counter:D2}-abcdefghijklmnopqrstuvwxyz0123456789";
    }

    public string HashToken(string token)
    {
        return $"hash::{token.Trim()}";
    }
}

internal sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<List<User>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Users.ToList());
    }

    public Task<List<User>> SearchAsync(string query, string excludedUserId, int limit, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();
        return Task.FromResult(
            Users
                .Where(user => !string.Equals(user.Id, excludedUserId, StringComparison.OrdinalIgnoreCase))
                .Where(user =>
                    user.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                    user.Email.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .OrderBy(user => user.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(user => user.Id, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList());
    }

    public Task<List<User>> ListByIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(Users.Where(user => ids.Contains(user.Id)).ToList());
    }

    public Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Users.FirstOrDefault(user => string.Equals(user.Id, userId, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Users.FirstOrDefault(user => string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)));
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }
}

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> RefreshTokens { get; } = [];

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RefreshTokens.FirstOrDefault(refreshToken => refreshToken.TokenHash == tokenHash));
    }

    public Task<List<RefreshToken>> ListByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(RefreshTokens.Where(refreshToken => refreshToken.UserId == userId).ToList());
    }

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        RefreshTokens.Add(refreshToken);
        return Task.CompletedTask;
    }
}

internal sealed class FakeItineraryRepository : IItineraryRepository
{
    public List<Itinerary> Itineraries { get; } = [];

    public Task<List<Itinerary>> ListAccessibleAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Itineraries.Where(itinerary => itinerary.Members.Any(member => member.UserId == userId)).ToList());
    }

    public Task<Itinerary?> GetByIdAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Itineraries.FirstOrDefault(itinerary => itinerary.Id == itineraryId));
    }

    public Task<Itinerary?> GetAccessibleByIdAsync(string userId, string itineraryId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Itineraries.FirstOrDefault(itinerary =>
            itinerary.Id == itineraryId && itinerary.Members.Any(member => member.UserId == userId)));
    }

    public Task<bool> IsMemberAsync(string itineraryId, string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Itineraries.Any(itinerary =>
            itinerary.Id == itineraryId && itinerary.Members.Any(member => member.UserId == userId)));
    }

    public Task<List<ItineraryMember>> ListMembersAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Itineraries.First(itinerary => itinerary.Id == itineraryId).Members.ToList());
    }

    public Task AddAsync(Itinerary itinerary, CancellationToken cancellationToken = default)
    {
        Itineraries.Add(itinerary);
        return Task.CompletedTask;
    }

    public Task AddMembersAsync(IEnumerable<ItineraryMember> members, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void RemoveMembers(IEnumerable<ItineraryMember> members)
    {
        foreach (var member in members.ToList())
        {
            var itinerary = Itineraries.First(itineraryItem => itineraryItem.Id == member.ItineraryId);
            itinerary.Members.Remove(member);
        }
    }
}

internal sealed class FakeEventRepository : IEventRepository
{
    public List<Event> Events { get; } = [];
    public List<EventAuditLog> AuditLogs { get; } = [];

    public Task<List<Event>> ListByItineraryAsync(string itineraryId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Events.Where(eventEntity => eventEntity.ItineraryId == itineraryId).ToList());
    }

    public Task<Event?> GetByIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Events.FirstOrDefault(eventEntity => eventEntity.Id == eventId));
    }

    public Task AddAsync(Event eventEntity, CancellationToken cancellationToken = default)
    {
        Events.Add(eventEntity);
        return Task.CompletedTask;
    }

    public void Remove(Event eventEntity)
    {
        Events.Remove(eventEntity);
    }

    public Task AddAuditLogAsync(EventAuditLog auditLog, CancellationToken cancellationToken = default)
    {
        AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    public Task<List<EventAuditLog>> ListHistoryAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AuditLogs.Where(log => log.EventId == eventId).ToList());
    }

    public Task<string?> GetHistoryItineraryIdAsync(string eventId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AuditLogs.FirstOrDefault(log => log.EventId == eventId)?.ItineraryId);
    }
}

internal static class TestDataFactory
{
    public static User CreateUser(string id, string name, string email, string avatar = "AA")
    {
        return new User
        {
            Id = id,
            ConcurrencyToken = $"{id}-v1",
            AuthVersion = $"{id}-auth-v1",
            Name = name,
            Email = email,
            PasswordHash = "hashed::Pass12345!",
            Avatar = avatar,
            CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    public static Itinerary CreateItinerary(
        string id,
        string createdById,
        string title = "Trip",
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        return new Itinerary
        {
            Id = id,
            ConcurrencyToken = $"{id}-v1",
            Title = title,
            Destination = "Tokyo",
            Description = "Test itinerary",
            StartDate = startDate ?? new DateOnly(2026, 4, 14),
            EndDate = endDate ?? new DateOnly(2026, 4, 18),
            CreatedById = createdById,
            CreatedAtUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    public static ItineraryMember CreateMember(Itinerary itinerary, User user, string? addedByUserId = null)
    {
        return new ItineraryMember
        {
            ItineraryId = itinerary.Id,
            UserId = user.Id,
            User = user,
            AddedByUserId = addedByUserId ?? itinerary.CreatedById,
            AddedAtUtc = new DateTime(2026, 2, 1, 1, 0, 0, DateTimeKind.Utc)
        };
    }

    public static Event CreateEvent(
        string id,
        string itineraryId,
        string createdById,
        string updatedById,
        string title = "Dinner")
    {
        return new Event
        {
            Id = id,
            ConcurrencyToken = $"{id}-v1",
            ItineraryId = itineraryId,
            Title = title,
            Description = "Test event",
            Category = EventCategory.Activity,
            Color = "#000000",
            StartDateTimeLocal = new DateTime(2026, 4, 15, 10, 0, 0, DateTimeKind.Unspecified),
            EndDateTimeLocal = new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Unspecified),
            Timezone = "Asia/Tokyo",
            Location = "Tokyo",
            LocationAddress = "Tokyo Station",
            LocationLat = 35.0m,
            LocationLng = 139.0m,
            Cost = 25m,
            CurrencyCode = "JPY",
            CreatedById = createdById,
            UpdatedById = updatedById,
            CreatedAtUtc = new DateTime(2026, 2, 1, 2, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 2, 1, 2, 0, 0, DateTimeKind.Utc)
        };
    }

    public static EventAuditLog CreateAuditLog(Event eventEntity, EventAuditAction action, string summary)
    {
        return new EventAuditLog
        {
            Id = Guid.NewGuid().ToString("N"),
            EventId = eventEntity.Id,
            ItineraryId = eventEntity.ItineraryId,
            Action = action,
            Summary = summary,
            SnapshotJson = JsonSerializer.Serialize(eventEntity.ToAuditSnapshot(), AuditJson.Options),
            ChangedByUserId = eventEntity.UpdatedById,
            ChangedAtUtc = eventEntity.UpdatedAtUtc
        };
    }
}
