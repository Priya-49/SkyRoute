using Microsoft.Extensions.Caching.Memory;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;

namespace SkyRoute.Infrastructure.Caching;

public sealed class FlightSearchCache : IFlightSearchCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly TimeSpan _entryTtl;

    public FlightSearchCache(IMemoryCache memoryCache, TimeSpan entryTtl)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        if (entryTtl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(entryTtl), "Entry TTL must be greater than zero.");
        }

        _entryTtl = entryTtl;
    }

    public void Store(Guid flightId, CachedFlightEntry entry)
    {
        if (flightId == Guid.Empty)
        {
            throw new ArgumentException("Flight id cannot be empty.", nameof(flightId));
        }

        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        _memoryCache.Set(
            flightId,
            entry,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _entryTtl
            });
    }

    public CachedFlightEntry? Get(Guid flightId)
    {
        if (flightId == Guid.Empty)
        {
            return null;
        }

        return _memoryCache.TryGetValue(flightId, out CachedFlightEntry? entry) ? entry : null;
    }
}
