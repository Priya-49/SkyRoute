using SkyRoute.Domain.Enums;

namespace SkyRoute.Application.Caching;

public sealed record CachedFlightEntry(
    Guid FlightId,
    string Provider,
    string FlightNumber,
    string Origin,
    string Destination,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    CabinClass CabinClass,
    decimal BaseFare);
