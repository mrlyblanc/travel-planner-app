using TravelPlannerApp.Application.Contracts.Auth;
using TravelPlannerApp.Application.Contracts.Users;

namespace TravelPlannerApp.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
