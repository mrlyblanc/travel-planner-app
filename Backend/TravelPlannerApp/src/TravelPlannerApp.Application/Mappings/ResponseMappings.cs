using System.Text.Json;
using TravelPlannerApp.Application.Common.Utilities;
using TravelPlannerApp.Application.Contracts.Events;
using TravelPlannerApp.Application.Contracts.Itineraries;
using TravelPlannerApp.Application.Contracts.Users;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Mappings;

public static class ResponseMappings
{
    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse(user.Id, user.ConcurrencyToken, user.Name, user.Email, user.Avatar, user.CreatedAtUtc);
    }

    public static UserLookupResponse ToLookupResponse(this User user)
    {
        return new UserLookupResponse(user.Id, user.Name, user.Email, user.Avatar);
    }

    public static ItineraryResponse ToResponse(this Itinerary itinerary)
    {
        return new ItineraryResponse(
            itinerary.Id,
            itinerary.ConcurrencyToken,
            itinerary.Title,
            itinerary.Description,
            itinerary.Destination,
            itinerary.StartDate,
            itinerary.EndDate,
            itinerary.CreatedById,
            itinerary.Members.Count,
            itinerary.CreatedAtUtc,
            itinerary.UpdatedAtUtc);
    }

    public static ItineraryMemberResponse ToResponse(this ItineraryMember member)
    {
        return new ItineraryMemberResponse(
            member.ItineraryId,
            member.UserId,
            member.User?.Name ?? string.Empty,
            member.User?.Email ?? string.Empty,
            member.User?.Avatar ?? string.Empty,
            member.AddedByUserId,
            member.AddedAtUtc);
    }

    public static EventResponse ToResponse(this Event eventEntity)
    {
        return new EventResponse(
            eventEntity.Id,
            eventEntity.ConcurrencyToken,
            eventEntity.ItineraryId,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.Category,
            eventEntity.Color,
            eventEntity.StartDateTimeLocal,
            eventEntity.EndDateTimeLocal,
            eventEntity.Timezone,
            eventEntity.Location,
            eventEntity.LocationAddress,
            eventEntity.LocationLat,
            eventEntity.LocationLng,
            eventEntity.Cost,
            eventEntity.CreatedById,
            eventEntity.UpdatedById,
            eventEntity.CreatedAtUtc,
            eventEntity.UpdatedAtUtc);
    }

    public static EventAuditSnapshotResponse ToAuditSnapshot(this Event eventEntity)
    {
        return new EventAuditSnapshotResponse(
            eventEntity.Id,
            eventEntity.ItineraryId,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.Category,
            eventEntity.Color,
            eventEntity.StartDateTimeLocal,
            eventEntity.EndDateTimeLocal,
            eventEntity.Timezone,
            eventEntity.Location,
            eventEntity.LocationAddress,
            eventEntity.LocationLat,
            eventEntity.LocationLng,
            eventEntity.Cost,
            eventEntity.UpdatedById,
            eventEntity.UpdatedAtUtc);
    }

    public static EventAuditLogResponse ToResponse(this EventAuditLog log)
    {
        var snapshot = JsonSerializer.Deserialize<EventAuditSnapshotResponse>(log.SnapshotJson, AuditJson.Options)
            ?? throw new InvalidOperationException("Audit snapshot could not be deserialized.");

        return new EventAuditLogResponse(
            log.Id,
            log.EventId,
            log.ItineraryId,
            log.Action,
            log.Summary,
            snapshot,
            log.ChangedByUserId,
            log.ChangedAtUtc);
    }
}
