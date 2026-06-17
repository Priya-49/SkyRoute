using FluentValidation;
using SkyRoute.Application.Flights;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Application;

public sealed class SearchFlightsUseCaseFanOutTests
{
    [Fact]
    public async Task ExecuteAsync_CallsAllProviders_AndMergesResults()
    {
        var providerOne = new CountingProvider("GlobalAir", 180m);
        var providerTwo = new CountingProvider("BudgetWings", 120m);
        var validator = new SearchFlightsQueryValidator();
        var useCase = new SearchFlightsUseCase(new IFlightProvider[] { providerOne, providerTwo }, validator);
        var query = BuildQuery();

        var results = await useCase.ExecuteAsync(query);

        Assert.Equal(1, providerOne.CallCount);
        Assert.Equal(1, providerTwo.CallCount);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsValidationException_ForInvalidQuery()
    {
        var provider = new CountingProvider("GlobalAir", 180m);
        var useCase = new SearchFlightsUseCase(new IFlightProvider[] { provider }, new SearchFlightsQueryValidator());
        var query = BuildQuery();
        query.Origin = "BAD";

        await Assert.ThrowsAsync<ValidationException>(() => useCase.ExecuteAsync(query));
    }

    private static SearchFlightsQuery BuildQuery() =>
        new()
        {
            Origin = "JFK",
            Destination = "LHR",
            DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
            Passengers = 2,
            CabinClass = "Economy"
        };

    private sealed class CountingProvider : IFlightProvider
    {
        private readonly decimal _baseFare;

        public CountingProvider(string providerName, decimal baseFare)
        {
            ProviderName = providerName;
            _baseFare = baseFare;
        }

        public string ProviderName { get; }

        public int CallCount { get; private set; }

        public Task<IReadOnlyCollection<Flight>> SearchAsync(
            FlightSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            var departureTime = criteria.DepartureDate.ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);
            var flight = new Flight(
                Guid.NewGuid(),
                $"{ProviderName[..2].ToUpperInvariant()}-100",
                ProviderName,
                criteria.Origin,
                criteria.Destination,
                departureTime,
                departureTime.AddHours(6),
                criteria.CabinClass,
                _baseFare);

            return Task.FromResult<IReadOnlyCollection<Flight>>(new[] { flight });
        }
    }
}
