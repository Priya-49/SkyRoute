using SkyRoute.Domain.Enums;

namespace SkyRoute.Domain.Entities;

public sealed class Booking
{
    public Guid Id { get; init; }

    public required string ReferenceCode { get; init; }

    public required string FlightNumber { get; init; }

    public required string Provider { get; init; }

    public required string Origin { get; init; }

    public required string Destination { get; init; }

    public DateTime DepartureTime { get; init; }

    public DateTime ArrivalTime { get; init; }

    public CabinClass CabinClass { get; init; }

    public required string PassengerName { get; init; }

    public required string Email { get; init; }

    public DocumentType DocumentType { get; init; }

    public required string DocumentNumber { get; init; }

    public int Passengers { get; init; }

    public decimal PricePerPassenger { get; init; }

    public decimal TotalPrice { get; init; }

    public DateTime CreatedAt { get; init; }
}
