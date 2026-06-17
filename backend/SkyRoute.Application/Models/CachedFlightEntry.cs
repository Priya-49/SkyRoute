using SkyRoute.Domain.Entities;

namespace SkyRoute.Application.Models;

public sealed class CachedFlightEntry
{
    public CachedFlightEntry(
        Guid flightId,
        string provider,
        string flightNumber,
        Airport origin,
        Airport destination,
        DateTime departureTime,
        DateTime arrivalTime,
        string cabinClass,
        decimal baseFare,
        DateTime expiresAtUtc)
    {
        if (flightId == Guid.Empty)
        {
            throw new ArgumentException("Flight id cannot be empty.", nameof(flightId));
        }

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ArgumentException("Provider cannot be null or whitespace.", nameof(provider));
        }

        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            throw new ArgumentException("Flight number cannot be null or whitespace.", nameof(flightNumber));
        }

        if (string.IsNullOrWhiteSpace(cabinClass))
        {
            throw new ArgumentException("Cabin class cannot be null or whitespace.", nameof(cabinClass));
        }

        if (baseFare <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseFare), "Base fare must be greater than zero.");
        }

        if (arrivalTime <= departureTime)
        {
            throw new ArgumentException("Arrival time must be after departure time.");
        }

        FlightId = flightId;
        Provider = provider.Trim();
        FlightNumber = flightNumber.Trim();
        Origin = origin ?? throw new ArgumentNullException(nameof(origin));
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
        CabinClass = cabinClass.Trim();
        BaseFare = baseFare;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid FlightId { get; }

    public string Provider { get; }

    public string FlightNumber { get; }

    public Airport Origin { get; }

    public Airport Destination { get; }

    public DateTime DepartureTime { get; }

    public DateTime ArrivalTime { get; }

    public string CabinClass { get; }

    public decimal BaseFare { get; }

    public DateTime ExpiresAtUtc { get; }

    public bool IsExpiredAt(DateTime utcNow) => utcNow >= ExpiresAtUtc;
}
