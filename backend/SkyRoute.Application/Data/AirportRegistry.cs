using SkyRoute.Domain.Entities;

namespace SkyRoute.Application.Data;

/// <summary>
/// In-memory registry of all airports supported by SkyRoute.
/// Adding a new airport here is the only change required to support new routes.
/// </summary>
public static class AirportRegistry
{
    private static readonly IReadOnlyDictionary<string, Airport> Airports =
        new Dictionary<string, Airport>(StringComparer.OrdinalIgnoreCase)
        {
            // India (5)
            ["DEL"] = new Airport("DEL", "Indira Gandhi International", "Delhi", "IN"),
            ["BOM"] = new Airport("BOM", "Chhatrapati Shivaji Maharaj International", "Mumbai", "IN"),
            ["BLR"] = new Airport("BLR", "Kempegowda International", "Bengaluru", "IN"),
            ["MAA"] = new Airport("MAA", "Chennai International", "Chennai", "IN"),
            ["HYD"] = new Airport("HYD", "Rajiv Gandhi International", "Hyderabad", "IN"),

            // International (5)
            ["JFK"] = new Airport("JFK", "John F. Kennedy International", "New York", "US"),
            ["LHR"] = new Airport("LHR", "Heathrow", "London", "GB"),
            ["DXB"] = new Airport("DXB", "Dubai International", "Dubai", "AE"),
            ["CDG"] = new Airport("CDG", "Charles de Gaulle", "Paris", "FR"),
            ["SIN"] = new Airport("SIN", "Singapore Changi", "Singapore", "SG"),
        };

    public static IReadOnlyDictionary<string, Airport> All => Airports;

    public static bool TryGet(string iataCode, out Airport? airport) =>
        Airports.TryGetValue(iataCode, out airport);

    public static bool IsKnownCode(string iataCode) =>
        Airports.ContainsKey(iataCode);
}
