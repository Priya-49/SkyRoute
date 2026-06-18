namespace SkyRoute.API.Contracts.Bookings;

public sealed class CreateBookingRequest
{
    public Guid FlightId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string FlightNumber { get; set; } = string.Empty;

    public string Origin { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public string CabinClass { get; set; } = string.Empty;

    public int Passengers { get; set; }

    public string PassengerName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DocumentType { get; set; } = string.Empty;

    public string DocumentNumber { get; set; } = string.Empty;
}
