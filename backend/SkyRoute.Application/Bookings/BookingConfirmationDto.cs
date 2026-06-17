namespace SkyRoute.Application.Bookings;

public sealed class BookingConfirmationDto
{
    public string ReferenceCode { get; init; } = string.Empty;

    public string PassengerName { get; init; } = string.Empty;

    public string Provider { get; init; } = string.Empty;

    public string FlightNumber { get; init; } = string.Empty;

    public string Origin { get; init; } = string.Empty;

    public string Destination { get; init; } = string.Empty;

    public DateTime DepartureTime { get; init; }

    public DateTime ArrivalTime { get; init; }

    public string CabinClass { get; init; } = string.Empty;

    public int Passengers { get; init; }

    public decimal PricePerPassenger { get; init; }

    public decimal TotalPrice { get; init; }
}
