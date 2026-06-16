using SkyRoute.Domain.Enums;

namespace SkyRoute.Domain.Entities;

public sealed class Flight
{
    public Guid Id { get; init; }

    public required string FlightNumber { get; init; }

    public required string Provider { get; init; }

    public required Airport Origin { get; init; }

    public required Airport Destination { get; init; }

    public DateTime DepartureTime { get; init; }

    public DateTime ArrivalTime { get; init; }

    public CabinClass CabinClass { get; init; }

    public decimal BaseFare { get; init; }
}
