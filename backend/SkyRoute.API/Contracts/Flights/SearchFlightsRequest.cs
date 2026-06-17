namespace SkyRoute.API.Contracts.Flights;

public sealed class SearchFlightsRequest
{
    public string Origin { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public DateOnly DepartureDate { get; set; }

    public int Passengers { get; set; }

    public string CabinClass { get; set; } = string.Empty;
}
