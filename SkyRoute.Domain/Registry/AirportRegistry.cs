using SkyRoute.Domain.Entities;

namespace SkyRoute.Domain.Registry;

public static class AirportRegistry
{
    public static IReadOnlyList<Airport> All { get; } =
    [
        new("JFK", "John F. Kennedy International", "New York", "US"),
        new("LAX", "Los Angeles International", "Los Angeles", "US"),
        new("ORD", "O'Hare International", "Chicago", "US"),
        new("LHR", "Heathrow", "London", "GB"),
        new("CDG", "Charles de Gaulle", "Paris", "FR"),
        new("DXB", "Dubai International", "Dubai", "AE")
    ];

    public static Airport? FindByCode(string code) =>
        All.FirstOrDefault(a => string.Equals(a.Code, code, StringComparison.OrdinalIgnoreCase));
}
