using Microsoft.AspNetCore.SignalR;
using TravelPlannerApp.Application.Abstractions.Realtime;

namespace TravelPlannerApp.Api.Realtime;

public sealed class SignalRItineraryRealtimeNotifier : IItineraryRealtimeNotifier
{
    private readonly IHubContext<ItineraryHub> _hubContext;

    public SignalRItineraryRealtimeNotifier(IHubContext<ItineraryHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyAsync(string itineraryId, ItineraryRealtimeNotification notification, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(ItineraryHub.GroupName(itineraryId))
            .SendAsync("itineraryUpdated", notification, cancellationToken);
    }
}
