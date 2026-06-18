namespace SkyRoute.API.Contracts.Bookings;

public sealed class CreateBookingResponse
{
    public string ReferenceCode { get; set; } = string.Empty;

    public string PassengerName { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string FlightNumber { get; set; } = string.Empty;

    public string Origin { get; set; } = string.Empty;

    public string Destination { get; set; } = string.Empty;

    public DateTime DepartureTime { get; set; }

    public DateTime ArrivalTime { get; set; }

    public string CabinClass { get; set; } = string.Empty;

    public int Passengers { get; set; }

    public string PricePerPassenger { get; set; } = string.Empty;

    public string TotalPrice { get; set; } = string.Empty;
}
