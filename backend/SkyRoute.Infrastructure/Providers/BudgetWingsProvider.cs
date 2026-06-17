using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Budget carrier offering two daily departures per route.
/// Lower base fares; BudgetWingsPricingStrategy applies a 10 % discount (min $29.99).
/// </summary>
public sealed class BudgetWingsProvider : IFlightProvider
{
    // Two daily departure slots (UTC)
    private static readonly TimeOnly[] DepartureTimes =
    [
        new TimeOnly(9, 0),
        new TimeOnly(17, 0),
    ];

    public string ProviderName => "BudgetWings";

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
                    $"BW-{routeBase + index}",
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
            "Business"   => isDomestic ? 160m  : 600m,
            "FirstClass" => isDomestic ? 400m  : 1600m,
            _            => isDomestic ? 60m   : 200m,   // Economy (default)
        };
    }
}
