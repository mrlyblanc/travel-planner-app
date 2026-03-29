namespace TravelPlannerApp.Api.Realtime;

public sealed class ItineraryHub : Microsoft.AspNetCore.SignalR.Hub
{
    public Task JoinItinerary(string itineraryId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, GroupName(itineraryId));
    }

    public Task LeaveItinerary(string itineraryId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(itineraryId));
    }

    public static string GroupName(string itineraryId) => $"itinerary:{itineraryId}";
}
