using Microsoft.Extensions.DependencyInjection;
using TravelPlannerApp.Application.Services;

namespace TravelPlannerApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IItineraryService, ItineraryService>();
        services.AddScoped<IEventService, EventService>();
        return services;
    }
}
