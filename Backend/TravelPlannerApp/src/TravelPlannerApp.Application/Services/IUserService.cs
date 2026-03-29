using TravelPlannerApp.Application.Contracts.Users;

namespace TravelPlannerApp.Application.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
}
