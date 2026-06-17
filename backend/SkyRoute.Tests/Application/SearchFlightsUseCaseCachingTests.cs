using SkyRoute.Application.Flights;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Application;

public sealed class SearchFlightsUseCaseCachingTests
{
    [Fact]
    public async Task ExecuteAsync_StoresEveryResultInCache()
    {
        var cache = new RecordingCache();
        var provider = new SingleFlightProvider();
        var useCase = new SearchFlightsUseCase(new IFlightProvider[] { provider }, new SearchFlightsQueryValidator(), cache);

        var results = await useCase.ExecuteAsync(new SearchFlightsQuery
        {
            Origin = "JFK",
            Destination = "LHR",
            DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(14)),
            Passengers = 2,
            CabinClass = "Economy"
        });

        Assert.Single(results);
        Assert.Single(cache.StoredEntries);
        Assert.Equal(results.Single().FlightId, cache.StoredEntries.Single().FlightId);
    }

    private sealed class RecordingCache : IFlightSearchCache
    {
        public List<CachedFlightEntry> StoredEntries { get; } = new();

        public void Store(Guid flightId, CachedFlightEntry entry)
        {
            StoredEntries.Add(entry);
        }

        public CachedFlightEntry? Get(Guid flightId) => null;
    }

    private sealed class SingleFlightProvider : IFlightProvider
    {
        private static readonly Airport Origin = new("JFK", "John F. Kennedy International", "New York", "US");
        private static readonly Airport Destination = new("LHR", "Heathrow", "London", "GB");

        public string ProviderName => "GlobalAir";

        public Task<IReadOnlyCollection<Flight>> SearchAsync(FlightSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            var departure = criteria.DepartureDate.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);
            var flight = new Flight(
                Guid.NewGuid(),
                "GA-123",
                ProviderName,
                Origin,
                Destination,
                departure,
                departure.AddHours(7),
                criteria.CabinClass,
                320m);

            return Task.FromResult<IReadOnlyCollection<Flight>>(new[] { flight });
        }
    }
}
