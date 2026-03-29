namespace TravelPlannerApp.Application.Abstractions.Realtime;

public interface IItineraryRealtimeNotifier
{
    Task NotifyAsync(string itineraryId, ItineraryRealtimeNotification notification, CancellationToken cancellationToken = default);
}
