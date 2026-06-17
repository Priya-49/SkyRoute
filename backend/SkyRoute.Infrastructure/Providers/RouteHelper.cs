using SkyRoute.Domain.Entities;

namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Shared route utilities used by flight providers.
/// Centralised so all providers agree on durations and flight number generation.
/// </summary>
internal static class RouteHelper
{
    // Approximate flight durations in minutes keyed by a sorted country-pair string.
    private static readonly IReadOnlyDictionary<string, int> RouteDurations =
        new Dictionary<string, int>
        {
            [SortedKey("IN", "IN")] = 120,   // domestic India
            [SortedKey("IN", "AE")] = 240,   // India – Dubai
            [SortedKey("IN", "SG")] = 330,   // India – Singapore
            [SortedKey("IN", "GB")] = 540,   // India – London
            [SortedKey("IN", "FR")] = 540,   // India – Paris
            [SortedKey("IN", "US")] = 840,   // India – New York
            [SortedKey("AE", "GB")] = 420,   // Dubai – London
            [SortedKey("AE", "FR")] = 420,   // Dubai – Paris
            [SortedKey("AE", "SG")] = 420,   // Dubai – Singapore
            [SortedKey("AE", "US")] = 840,   // Dubai – New York
            [SortedKey("SG", "GB")] = 780,   // Singapore – London
            [SortedKey("SG", "FR")] = 780,   // Singapore – Paris
            [SortedKey("SG", "US")] = 960,   // Singapore – New York
            [SortedKey("GB", "US")] = 420,   // London – New York
            [SortedKey("FR", "US")] = 480,   // Paris – New York
            [SortedKey("GB", "FR")] = 75,    // London – Paris
        };

    public static int EstimateDurationMinutes(Airport origin, Airport destination)
    {
        var key = SortedKey(origin.CountryCode, destination.CountryCode);
        return RouteDurations.GetValueOrDefault(key, 480); // 8h default for unknown pairs
    }

    /// <summary>
    /// Produces a deterministic 3-digit route number from an origin+destination pair
    /// so flight numbers look route-specific (e.g. GA-101, GA-102 for one route).
    /// </summary>
    public static int ComputeRouteBase(Airport origin, Airport destination)
    {
        var hash = 0;
        foreach (var c in origin.Code + destination.Code)
            hash = hash * 31 + c;
        return 100 + Math.Abs(hash) % 900; // 100–999
    }

    private static string SortedKey(string c1, string c2) =>
        string.Compare(c1, c2, StringComparison.Ordinal) < 0 ? $"{c1}-{c2}" : $"{c2}-{c1}";
}
