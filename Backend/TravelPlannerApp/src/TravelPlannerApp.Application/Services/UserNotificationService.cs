using TravelPlannerApp.Application.Abstractions.CurrentUser;
using TravelPlannerApp.Application.Abstractions.Persistence;
using TravelPlannerApp.Application.Common.Exceptions;
using TravelPlannerApp.Application.Contracts.Notifications;
using TravelPlannerApp.Application.Mappings;
using TravelPlannerApp.Domain.Entities;

namespace TravelPlannerApp.Application.Services;

public sealed class UserNotificationService : IUserNotificationService
{
    private readonly ICurrentUserAccessor _currentUserAccessor;
    private readonly IUserRepository _userRepository;
    private readonly IUserNotificationRepository _userNotificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserNotificationService(
        ICurrentUserAccessor currentUserAccessor,
        IUserRepository userRepository,
        IUserNotificationRepository userNotificationRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserAccessor = currentUserAccessor;
        _userRepository = userRepository;
        _userNotificationRepository = userNotificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<UserNotificationResponse>> GetCurrentUserNotificationsAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var notifications = await _userNotificationRepository.ListByUserIdAsync(currentUser.Id, cancellationToken: cancellationToken);
        return notifications
            .Select(static notification => notification.ToResponse())
            .ToList();
    }

    public async Task<UserNotificationResponse> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var notification = await _userNotificationRepository.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null || !string.Equals(notification.UserId, currentUser.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException($"Notification '{notificationId}' was not found.");
        }

        if (!notification.ReadAtUtc.HasValue)
        {
            notification.ReadAtUtc = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return notification.ToResponse();
    }

    public async Task<int> MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var notifications = await _userNotificationRepository.ListByUserIdAsync(currentUser.Id, cancellationToken: cancellationToken);
        var unreadNotifications = notifications
            .Where(static notification => !notification.ReadAtUtc.HasValue)
            .ToList();

        if (unreadNotifications.Count == 0)
        {
            return 0;
        }

        var readAtUtc = DateTime.UtcNow;
        foreach (var notification in unreadNotifications)
        {
            notification.ReadAtUtc = readAtUtc;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return unreadNotifications.Count;
    }

    public async Task DeleteAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(cancellationToken);
        var notification = await _userNotificationRepository.GetByIdAsync(notificationId, cancellationToken);
        if (notification is null || !string.Equals(notification.UserId, currentUser.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotFoundException($"Notification '{notificationId}' was not found.");
        }

        _userNotificationRepository.Remove(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserAccessor.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new UnauthorizedException("Authenticated user is required.");
        }

        return await _userRepository.GetByIdAsync(currentUserId.Trim(), cancellationToken)
            ?? throw new UnauthorizedException($"Current user '{currentUserId}' was not found.");
    }
}
