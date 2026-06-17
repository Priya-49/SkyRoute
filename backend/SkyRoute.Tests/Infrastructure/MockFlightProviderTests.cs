using SkyRoute.Domain.Entities;
using SkyRoute.Domain.ValueObjects;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Tests.Infrastructure;

public sealed class MockFlightProviderTests
{
    [Fact]
    public async Task SearchAsync_ReturnsHardcodedFlights()
    {
        var provider = new MockFlightProvider();
        var criteria = BuildCriteria();

        var flights = await provider.SearchAsync(criteria);

        Assert.Equal(2, flights.Count);
    }

    [Fact]
    public async Task SearchAsync_ReturnsValidFlightObjects()
    {
        var provider = new MockFlightProvider();
        var criteria = BuildCriteria();

        var flights = await provider.SearchAsync(criteria);

        Assert.All(flights, flight =>
        {
            Assert.Equal(provider.ProviderName, flight.Provider);
            Assert.Equal(criteria.Origin.Code, flight.Origin.Code);
            Assert.Equal(criteria.Destination.Code, flight.Destination.Code);
            Assert.True(flight.ArrivalTime > flight.DepartureTime);
            Assert.True(flight.BaseFare > 0);
        });
    }

    private static FlightSearchCriteria BuildCriteria()
    {
        var origin = new Airport("JFK", "John F. Kennedy International", "New York", "US");
        var destination = new Airport("LHR", "Heathrow", "London", "GB");

        return new FlightSearchCriteria(origin, destination, new DateOnly(2026, 8, 15), 2, "Economy");
    }
}
