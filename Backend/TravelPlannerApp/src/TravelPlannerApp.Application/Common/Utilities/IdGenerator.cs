namespace TravelPlannerApp.Application.Common.Utilities;

public static class IdGenerator
{
    public static string New(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        return $"{prefix}-{Guid.NewGuid():N}";
    }
}
