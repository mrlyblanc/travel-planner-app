namespace TravelPlannerApp.Application.Common.Utilities;

public static class AvatarGenerator
{
    public static string Generate(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "?";
        }

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return parts[0].Length >= 2
                ? parts[0][..2].ToUpperInvariant()
                : parts[0].ToUpperInvariant();
        }

        return string.Concat(parts.Take(2).Select(static part => char.ToUpperInvariant(part[0])));
    }
}
