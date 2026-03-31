using Microsoft.AspNetCore.SignalR;
using TravelPlannerApp.Api.Common.Security;
using TravelPlannerApp.Application.Abstractions.Persistence;

namespace TravelPlannerApp.Api.Realtime;

public sealed class ItineraryHub : Hub
{
    private readonly IItineraryRepository _itineraryRepository;

    public ItineraryHub(IItineraryRepository itineraryRepository)
    {
        _itineraryRepository = itineraryRepository;
    }

    public override async Task OnConnectedAsync()
    {
        var currentUserId = Context.User.GetCurrentUserId();
        if (!string.IsNullOrWhiteSpace(currentUserId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(currentUserId));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinItinerary(string itineraryId)
    {
        var currentUserId = Context.User.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new HubException("Authenticated user is required.");
        }

        var isMember = await _itineraryRepository.IsMemberAsync(itineraryId, currentUserId, Context.ConnectionAborted);
        if (!isMember)
        {
            throw new HubException("You do not have access to this itinerary.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(itineraryId));
    }

    public Task LeaveItinerary(string itineraryId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(itineraryId));
    }

    public static string GroupName(string itineraryId) => $"itinerary:{itineraryId}";

    public static string UserGroupName(string userId) => $"user:{userId}";
}
