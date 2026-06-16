using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.Models;
using SkyRoute.Domain.Registry;

namespace SkyRoute.Infrastructure.Providers;

public sealed class BudgetWingsProvider : IFlightProvider
{
    public string ProviderName => "BudgetWings";

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
                FlightNumber = "BW-1193",
                Origin = origin,
                Destination = destination,
                DepartureTime = CreateUtc(departureDate, 14, 30),
                ArrivalTime = CreateUtc(departureDate.AddDays(1), 2, 30),
                CabinClass = criteria.CabinClass,
                BaseFare = 159.00m
            },
            new Flight
            {
                Id = Guid.NewGuid(),
                Provider = ProviderName,
                FlightNumber = "BW-2248",
                Origin = origin,
                Destination = destination,
                DepartureTime = CreateUtc(departureDate, 6, 10),
                ArrivalTime = CreateUtc(departureDate, 17, 45),
                CabinClass = criteria.CabinClass,
                BaseFare = 132.40m
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
