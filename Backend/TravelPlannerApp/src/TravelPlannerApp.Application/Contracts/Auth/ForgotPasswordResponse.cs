namespace TravelPlannerApp.Application.Contracts.Auth;

public sealed record ForgotPasswordResponse(
    string Message,
    string? DevResetToken);
