using Microsoft.Extensions.Caching.Memory;
using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;
using SkyRoute.Infrastructure.Caching;

namespace SkyRoute.Tests.Infrastructure;

public sealed class FlightSearchCacheTests
{
    [Fact]
    public void Store_ThenGet_ReturnsCachedFlight()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new FlightSearchCache(memoryCache, TimeSpan.FromMinutes(5));
        var flightId = Guid.NewGuid();
        var entry = CreateEntry(flightId, DateTime.UtcNow.AddMinutes(5));

        cache.Store(flightId, entry);
        var cached = cache.Get(flightId);

        Assert.NotNull(cached);
        Assert.Equal(entry.FlightId, cached!.FlightId);
        Assert.Equal(entry.Provider, cached.Provider);
    }

    [Fact]
    public void Get_ReturnsNull_WhenEntryIsMissing()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new FlightSearchCache(memoryCache, TimeSpan.FromMinutes(5));

        var cached = cache.Get(Guid.NewGuid());

        Assert.Null(cached);
    }

    [Fact]
    public async Task Get_ReturnsNull_WhenEntryHasExpired()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new FlightSearchCache(memoryCache, TimeSpan.FromMilliseconds(50));
        var flightId = Guid.NewGuid();
        var entry = CreateEntry(flightId, DateTime.UtcNow.AddMilliseconds(50));

        cache.Store(flightId, entry);
        await Task.Delay(120);
        var cached = cache.Get(flightId);

        Assert.Null(cached);
    }

    [Fact]
    public void DefaultEntryTtl_IsThirtyMinutes()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), FlightSearchCache.DefaultEntryTtl);
    }

    private static CachedFlightEntry CreateEntry(Guid flightId, DateTime expiresAtUtc)
    {
        var origin = new Airport("JFK", "John F. Kennedy International", "New York", "US");
        var destination = new Airport("LHR", "Heathrow", "London", "GB");

        return new CachedFlightEntry(
            flightId,
            "GlobalAir",
            "GA-4821",
            origin,
            destination,
            new DateTime(2026, 8, 15, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 15, 20, 0, 0, DateTimeKind.Utc),
            "Economy",
            320m,
            expiresAtUtc);
    }
}
