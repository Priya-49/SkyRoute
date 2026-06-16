using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;
using SkyRoute.Domain.Registry;

namespace SkyRoute.Infrastructure.Providers;

public sealed class GlobalAirProvider : IFlightProvider
{
    public string ProviderName => "GlobalAir";

    public Task<IReadOnlyList<Flight>> SearchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var origin = GetAirport(criteria.Origin);
        var destination = GetAirport(criteria.Destination);
        var departureDate = criteria.DepartureDate;

        IReadOnlyList<Flight> flights =
        [
            new Flight
            {
                Id = Guid.NewGuid(),
                Provider = ProviderName,
                FlightNumber = "GA-4821",
                Origin = origin,
                Destination = destination,
                DepartureTime = CreateUtc(departureDate, 8, 0),
                ArrivalTime = CreateUtc(departureDate, 20, 0),
                CabinClass = criteria.CabinClass,
                BaseFare = 320.00m
            },
            new Flight
            {
                Id = Guid.NewGuid(),
                Provider = ProviderName,
                FlightNumber = "GA-6139",
                Origin = origin,
                Destination = destination,
                DepartureTime = CreateUtc(departureDate, 13, 15),
                ArrivalTime = CreateUtc(departureDate, 23, 40),
                CabinClass = criteria.CabinClass,
                BaseFare = 285.50m
            },
            new Flight
            {
                Id = Guid.NewGuid(),
                Provider = ProviderName,
                FlightNumber = "GA-7402",
                Origin = origin,
                Destination = destination,
                DepartureTime = CreateUtc(departureDate, 21, 45),
                ArrivalTime = CreateUtc(departureDate.AddDays(1), 7, 5),
                CabinClass = criteria.CabinClass,
                BaseFare = 352.75m
            }
        ];

        return Task.FromResult(flights);
    }

    private static Airport GetAirport(string code) =>
        AirportRegistry.FindByCode(code)
        ?? throw new InvalidOperationException($"Unknown airport code: {code}.");

    private static DateTime CreateUtc(DateOnly date, int hour, int minute) =>
        new(date.Year, date.Month, date.Day, hour, minute, 0, DateTimeKind.Utc);
}
