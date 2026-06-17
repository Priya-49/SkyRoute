using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Infrastructure.Providers;

public sealed class MockFlightProvider : IFlightProvider
{
    public string ProviderName => "MockFlightProvider";

    public Task<IReadOnlyCollection<Flight>> SearchAsync(FlightSearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var departureDateTime = criteria.DepartureDate.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc);
        var flights = new List<Flight>
        {
            new(
                Guid.NewGuid(),
                "MOCK-1001",
                ProviderName,
                criteria.Origin,
                criteria.Destination,
                departureDateTime,
                departureDateTime.AddHours(2),
                criteria.CabinClass,
                120m),
            new(
                Guid.NewGuid(),
                "MOCK-1002",
                ProviderName,
                criteria.Origin,
                criteria.Destination,
                departureDateTime.AddHours(4),
                departureDateTime.AddHours(6),
                criteria.CabinClass,
                180m)
        };

        return Task.FromResult<IReadOnlyCollection<Flight>>(flights);
    }
}
