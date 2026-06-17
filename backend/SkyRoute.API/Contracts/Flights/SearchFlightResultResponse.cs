namespace SkyRoute.API.Contracts.Flights;

public sealed class SearchFlightResultResponse
{
    public Guid FlightId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string FlightNumber { get; set; } = string.Empty;

    public string Origin { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public int DurationMinutes { get; set; }

    public string CabinClass { get; set; } = string.Empty;

    public string PricePerPassenger { get; set; } = string.Empty;

    public string TotalPrice { get; set; } = string.Empty;
}
