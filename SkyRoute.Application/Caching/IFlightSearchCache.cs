namespace SkyRoute.Application.Caching;

/// <summary>
/// Defines transient storage for search-result flight entries used by booking.
/// </summary>
public interface IFlightSearchCache
{
    /// <summary>
    /// Stores a cached entry for a flight id.
    /// </summary>
    /// <param name="flightId">Flight identifier used as cache key.</param>
    /// <param name="entry">Cached flight entry.</param>
    void Store(Guid flightId, CachedFlightEntry entry);

    /// <summary>
    /// Retrieves a cached flight entry by flight id.
    /// </summary>
    /// <param name="flightId">Flight identifier used as cache key.</param>
    /// <returns>Cached flight entry if found; otherwise null.</returns>
    CachedFlightEntry? Get(Guid flightId);
}
