using System.Reflection;
using System.Text.Json;

namespace TravelPlannerApp.Application.Common;

public sealed record CurrencyCatalogEntry(
    string Code,
    string Name,
    int MinorUnit);

public static class CurrencyCatalog
{
    private const string ResourceName = "TravelPlannerApp.Application.Resources.CurrencyCatalog.json";

    private static readonly Lazy<IReadOnlyDictionary<string, CurrencyCatalogEntry>> Entries = new(LoadEntries);

    public static bool TryGet(string? currencyCode, out CurrencyCatalogEntry? entry)
    {
        var normalizedCode = Normalize(currencyCode);
        if (normalizedCode is null)
        {
            entry = null;
            return false;
        }

        if (Entries.Value.TryGetValue(normalizedCode, out var resolvedEntry))
        {
            entry = resolvedEntry;
            return true;
        }

        entry = null;
        return false;
    }

    public static bool IsSupported(string? currencyCode) => TryGet(currencyCode, out _);

    public static bool SupportsAmount(decimal amount, string? currencyCode)
    {
        if (!TryGet(currencyCode, out var entry) || entry is null)
        {
            return false;
        }

        return decimal.Round(amount, entry.MinorUnit) == amount;
    }

    private static IReadOnlyDictionary<string, CurrencyCatalogEntry> LoadEntries()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded currency catalog resource '{ResourceName}' was not found.");

        var entries = JsonSerializer.Deserialize<List<CurrencyCatalogEntry>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];

        return entries
            .Select(entry => entry with { Code = entry.Code.Trim().ToUpperInvariant(), Name = entry.Name.Trim() })
            .ToDictionary(entry => entry.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static string? Normalize(string? currencyCode)
    {
        var normalizedCode = currencyCode?.Trim().ToUpperInvariant();
        return string.IsNullOrWhiteSpace(normalizedCode) ? null : normalizedCode;
    }
}
