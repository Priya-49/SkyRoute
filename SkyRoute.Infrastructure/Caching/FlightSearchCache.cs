using Microsoft.Extensions.Caching.Memory;
using SkyRoute.Application.Caching;

namespace SkyRoute.Infrastructure.Caching;

public sealed class FlightSearchCache(IMemoryCache memoryCache) : IFlightSearchCache
{
    private static readonly TimeSpan AbsoluteTtl = TimeSpan.FromMinutes(30);

    public void Store(Guid flightId, CachedFlightEntry entry)
    {
        memoryCache.Set(
            flightId,
            entry,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AbsoluteTtl
            });
    }

    public CachedFlightEntry? Get(Guid flightId) =>
        memoryCache.TryGetValue(flightId, out CachedFlightEntry? entry)
            ? entry
            : null;
}
