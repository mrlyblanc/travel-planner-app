namespace TravelPlannerApp.Api.Common.Configuration;

public static class DotEnvLoader
{
    public static void Load()
    {
        foreach (var root in GetCandidateRoots().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var envPath = FindEnvPath(root);
            if (envPath is null)
            {
                continue;
            }

            foreach (var line in File.ReadLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = trimmed[..separatorIndex].Trim();
                var value = trimmed[(separatorIndex + 1)..].Trim();
                value = TrimQuotes(value);

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }
    }

    private static IEnumerable<string> GetCandidateRoots()
    {
        yield return Directory.GetCurrentDirectory();
        yield return AppContext.BaseDirectory;
    }

    private static string? FindEnvPath(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string TrimQuotes(string value)
    {
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
        {
            return value[1..^1];
        }

        if (value.Length >= 2 && value.StartsWith('\'') && value.EndsWith('\''))
        {
            return value[1..^1];
        }

        return value;
    }
}
