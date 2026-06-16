using SkyRoute.Domain.Enums;

namespace SkyRoute.Domain.Models;

public sealed record FlightSearchCriteria(
    string Origin,
    string Destination,
    DateOnly DepartureDate,
    int Passengers,
    CabinClass CabinClass);
