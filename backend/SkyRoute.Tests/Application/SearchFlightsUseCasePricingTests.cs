using SkyRoute.Application.Flights;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Application;

public sealed class SearchFlightsUseCasePricingTests
{
    [Fact]
    public async Task ExecuteAsync_AppliesProviderPricingStrategy()
    {
        var provider = new SingleFlightProvider("GlobalAir", 320m);
        var useCase = new SearchFlightsUseCase(
            new IFlightProvider[] { provider },
            new SearchFlightsQueryValidator(),
            new NoOpCache(),
            new SkyRoute.Application.Interfaces.IPricingStrategy[] { new FixedPricingStrategy("GlobalAir", 368m) });

        var result = await useCase.ExecuteAsync(new SearchFlightsQuery
        {
            Origin = "JFK",
            Destination = "LHR",
            DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(10)),
            Passengers = 2,
            CabinClass = "Economy"
        });

        var flight = Assert.Single(result);
        Assert.Equal(368m, flight.PricePerPassenger);
        Assert.Equal(736m, flight.TotalPrice);
    }

    private sealed class SingleFlightProvider : IFlightProvider
    {
        private readonly decimal _baseFare;

        public SingleFlightProvider(string providerName, decimal baseFare)
        {
            ProviderName = providerName;
            _baseFare = baseFare;
        }

        public string ProviderName { get; }

        public Task<IReadOnlyCollection<Flight>> SearchAsync(FlightSearchCriteria criteria, CancellationToken cancellationToken = default)
        {
            var departure = criteria.DepartureDate.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);
            var flight = new Flight(
                Guid.NewGuid(),
                "GA-4821",
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

    private sealed class FixedPricingStrategy : SkyRoute.Application.Interfaces.IPricingStrategy
    {
        private readonly decimal _result;

        public FixedPricingStrategy(string providerName, decimal result)
        {
            ProviderName = providerName;
            _result = result;
        }

        public string ProviderName { get; }

        public decimal Calculate(decimal baseFare) => _result;
    }

    private sealed class NoOpCache : IFlightSearchCache
    {
        public void Store(Guid flightId, CachedFlightEntry entry)
        {
        }

        public CachedFlightEntry? Get(Guid flightId) => null;
    }
}
