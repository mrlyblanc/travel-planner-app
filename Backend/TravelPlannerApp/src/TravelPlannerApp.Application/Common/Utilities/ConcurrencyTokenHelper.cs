using TravelPlannerApp.Application.Common.Exceptions;

namespace TravelPlannerApp.Application.Common.Utilities;

public static class ConcurrencyTokenHelper
{
    public static string NewToken()
    {
        return Guid.NewGuid().ToString("N");
    }

    public static string ToETag(string token)
    {
        var normalized = Normalize(token);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("A concurrency token is required.", nameof(token));
        }

        return $"\"{normalized}\"";
    }

    public static void EnsureMatches(string currentToken, string? expectedToken)
    {
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            throw new PreconditionRequiredException("If-Match header is required for this operation.");
        }

        var normalizedCurrent = Normalize(currentToken);
        var normalizedExpected = Normalize(expectedToken);
        if (string.IsNullOrWhiteSpace(normalizedExpected) || normalizedExpected == "*")
        {
            throw new BadRequestException("If-Match must contain the current resource version.");
        }

        if (!string.Equals(normalizedCurrent, normalizedExpected, StringComparison.Ordinal))
        {
            throw new PreconditionFailedException("The resource was modified by another user. Refresh and retry.");
        }
    }

    public static string Normalize(string token)
    {
        var normalized = token.Trim();
        if (normalized.StartsWith("W/\"", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..].TrimStart();
        }

        if (normalized.Length >= 2 && normalized.StartsWith('"') && normalized.EndsWith('"'))
        {
            normalized = normalized[1..^1];
        }

        return normalized.Trim();
    }
}
