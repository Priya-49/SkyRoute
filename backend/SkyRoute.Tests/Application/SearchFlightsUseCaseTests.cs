using FluentValidation;
using SkyRoute.Application.Flights;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Application;

public sealed class SearchFlightsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ValidatesInput()
    {
        var useCase = CreateUseCase(new IFlightProvider[] { new FakeProvider("GlobalAir", 320m) }, out _);
        var invalidQuery = BuildValidQuery();
        invalidQuery.Origin = "BAD";

        await Assert.ThrowsAsync<ValidationException>(() => useCase.ExecuteAsync(invalidQuery));
    }

    [Fact]
    public async Task ExecuteAsync_CallsAllRegisteredProviders()
    {
        var providerOne = new FakeProvider("GlobalAir", 320m);
        var providerTwo = new FakeProvider("BudgetWings", 200m);
        var useCase = CreateUseCase(new IFlightProvider[] { providerOne, providerTwo }, out _);

        var results = await useCase.ExecuteAsync(BuildValidQuery());

        Assert.Equal(1, providerOne.CallCount);
        Assert.Equal(1, providerTwo.CallCount);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ExecuteAsync_AppliesPricingStrategy()
    {
        var useCase = CreateUseCase(new IFlightProvider[] { new FakeProvider("GlobalAir", 320m) }, out _);

        var results = await useCase.ExecuteAsync(BuildValidQuery());
        var flight = Assert.Single(results);

        Assert.Equal(368m, flight.PricePerPassenger);
        Assert.Equal(736m, flight.TotalPrice);
    }

    [Fact]
    public async Task ExecuteAsync_StoresResultsInCache()
    {
        var useCase = CreateUseCase(new IFlightProvider[] { new FakeProvider("GlobalAir", 320m) }, out var cache);

        var results = await useCase.ExecuteAsync(BuildValidQuery());

        Assert.Single(results);
        Assert.Single(cache.Stored);
        Assert.Equal(results.Single().FlightId, cache.Stored.Single().FlightId);
    }

    private static SearchFlightsUseCase CreateUseCase(IEnumerable<IFlightProvider> providers, out RecordingCache cache)
    {
        cache = new RecordingCache();
        var strategies = new SkyRoute.Application.Interfaces.IPricingStrategy[]
        {
            new FixedResultPricing("GlobalAir", 368m),
            new FixedResultPricing("BudgetWings", 180m)
        };

        return new SearchFlightsUseCase(providers, new SearchFlightsQueryValidator(), cache, strategies);
    }

    private static SearchFlightsQuery BuildValidQuery() =>
        new()
        {
            Origin = "JFK",
            Destination = "LHR",
            DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            Passengers = 2,
            CabinClass = "Economy"
        };

    private sealed class FakeProvider : IFlightProvider
    {
        private readonly decimal _baseFare;

        public FakeProvider(string providerName, decimal baseFare)
        {
            ProviderName = providerName;
            _baseFare = baseFare;
        }

        public string ProviderName { get; }

        public int CallCount { get; private set; }

        public Task<IReadOnlyCollection<Flight>> SearchAsync(FlightSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            CallCount++;
            var departure = criteria.DepartureDate.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);
            var flight = new Flight(
                Guid.NewGuid(),
                $"{ProviderName[..2].ToUpperInvariant()}-100",
                ProviderName,
                criteria.Origin,
                criteria.Destination,
                departure,
                departure.AddHours(7),
                criteria.CabinClass,
                _baseFare);

            return Task.FromResult<IReadOnlyCollection<Flight>>(new[] { flight });
        }
    }

    private sealed class RecordingCache : IFlightSearchCache
    {
        public List<CachedFlightEntry> Stored { get; } = new();

        public void Store(Guid flightId, CachedFlightEntry entry)
        {
            Stored.Add(entry);
        }

        public CachedFlightEntry? Get(Guid flightId) => null;
    }

    private sealed class FixedResultPricing : SkyRoute.Application.Interfaces.IPricingStrategy
    {
        private readonly decimal _result;

        public FixedResultPricing(string providerName, decimal result)
        {
            ProviderName = providerName;
            _result = result;
        }

        public string ProviderName { get; }

        public decimal Calculate(decimal baseFare) => _result;
    }
}
