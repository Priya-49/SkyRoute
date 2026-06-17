using SkyRoute.Domain.Entities;

namespace SkyRoute.Domain.ValueObjects;

public sealed class FlightSearchCriteria
{
    public FlightSearchCriteria(
        Airport origin,
        Airport destination,
        DateOnly departureDate,
        int passengers,
        string cabinClass)
    {
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));

        if (Origin.Code == Destination.Code)
        {
            throw new ArgumentException("Origin and destination must be different airports.");
        }

        if (passengers < 1 || passengers > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(passengers), "Passengers must be between 1 and 9.");
        }

        if (string.IsNullOrWhiteSpace(cabinClass))
        {
            throw new ArgumentException("Cabin class cannot be null or whitespace.", nameof(cabinClass));
        }

        DepartureDate = departureDate;
        Passengers = passengers;
        CabinClass = cabinClass.Trim();
    }

    public Airport Origin { get; }

    public Airport Destination { get; }

    public DateOnly DepartureDate { get; }

    public int Passengers { get; }

    public string CabinClass { get; }
}
