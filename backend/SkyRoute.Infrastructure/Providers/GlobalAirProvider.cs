using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Full-service carrier offering three daily departures per route.
/// Higher base fares, applied with a 15 % markup via GlobalAirPricingStrategy.
/// </summary>
public sealed class GlobalAirProvider : IFlightProvider
{
    // Three daily departure slots (UTC)
    private static readonly TimeOnly[] DepartureTimes =
    [
        new TimeOnly(6, 0),
        new TimeOnly(12, 0),
        new TimeOnly(20, 0),
    ];

    public string ProviderName => "GlobalAir";

    public Task<IReadOnlyCollection<Flight>> SearchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var durationMinutes = RouteHelper.EstimateDurationMinutes(criteria.Origin, criteria.Destination);
        var routeBase = RouteHelper.ComputeRouteBase(criteria.Origin, criteria.Destination);
        var baseFare = GetBaseFare(criteria.Origin, criteria.Destination, criteria.CabinClass);

        var flights = DepartureTimes
            .Select((time, index) =>
            {
                var departure = criteria.DepartureDate.ToDateTime(time, DateTimeKind.Utc);
                return new Flight(
                    Guid.NewGuid(),
                    $"GA-{routeBase + index}",
                    ProviderName,
                    criteria.Origin,
                    criteria.Destination,
                    departure,
                    departure.AddMinutes(durationMinutes),
                    criteria.CabinClass,
                    baseFare);
            })
            .ToList();

        return Task.FromResult<IReadOnlyCollection<Flight>>(flights);
    }

    private static decimal GetBaseFare(Airport origin, Airport destination, string cabinClass)
    {
        var isDomestic = origin.CountryCode == destination.CountryCode;
        return cabinClass switch
        {
            "Business"   => isDomestic ? 250m  : 900m,
            "FirstClass" => isDomestic ? 600m  : 2400m,
            _            => isDomestic ? 90m   : 320m,   // Economy (default)
        };
    }
}
