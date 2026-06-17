using SkyRoute.Domain.Entities;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Domain.Interfaces;

public interface IFlightProvider
{
    string ProviderName { get; }

    Task<IReadOnlyCollection<Flight>> SearchAsync(FlightSearchCriteria criteria, CancellationToken cancellationToken = default);
}
