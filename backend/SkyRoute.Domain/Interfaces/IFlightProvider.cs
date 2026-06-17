using SkyRoute.Domain.Entities;

namespace SkyRoute.Domain.Interfaces;

public interface IFlightProvider
{
    string ProviderName { get; }

    Task<IReadOnlyCollection<Flight>> SearchAsync(
        Airport origin,
        Airport destination,
        DateOnly departureDate,
        int passengers,
        string cabinClass,
        CancellationToken cancellationToken = default);
}
