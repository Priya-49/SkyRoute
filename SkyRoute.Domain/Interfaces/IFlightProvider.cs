using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Models;

namespace SkyRoute.Domain.Interfaces;

/// <summary>
/// Defines a flight search provider source.
/// </summary>
public interface IFlightProvider
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Searches available flights for the given criteria.
    /// </summary>
    /// <param name="criteria">Search criteria.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Provider flights matching the criteria.</returns>
    Task<IReadOnlyList<Flight>> SearchAsync(
        FlightSearchCriteria criteria,
        CancellationToken cancellationToken = default);
}
