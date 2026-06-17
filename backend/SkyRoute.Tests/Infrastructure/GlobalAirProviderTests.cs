using SkyRoute.Domain.Entities;
using SkyRoute.Domain.ValueObjects;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Tests.Infrastructure;

public sealed class GlobalAirProviderTests
{
    private static readonly Airport Delhi = new("DEL", "Indira Gandhi International", "Delhi", "IN");
    private static readonly Airport Mumbai = new("BOM", "Chhatrapati Shivaji Maharaj International", "Mumbai", "IN");
    private static readonly Airport London = new("LHR", "Heathrow", "London", "GB");

    [Fact]
    public async Task SearchAsync_ReturnsThreeFlights_ForAnyRoute()
    {
        var provider = new GlobalAirProvider();
        var criteria = new FlightSearchCriteria(Delhi, London, new DateOnly(2025, 8, 1), 1, "Economy");

        var flights = await provider.SearchAsync(criteria);

        Assert.Equal(3, flights.Count);
    }

    [Fact]
    public async Task SearchAsync_AllFlights_HaveCorrectProviderName()
    {
        var provider = new GlobalAirProvider();
        var criteria = new FlightSearchCriteria(Delhi, London, new DateOnly(2025, 8, 1), 1, "Economy");

        var flights = await provider.SearchAsync(criteria);

        Assert.All(flights, f => Assert.Equal("GlobalAir", f.Provider));
    }

    [Fact]
    public async Task SearchAsync_AllFlights_HaveGAPrefixedFlightNumbers()
    {
        var provider = new GlobalAirProvider();
        var criteria = new FlightSearchCriteria(Delhi, London, new DateOnly(2025, 8, 1), 1, "Economy");

        var flights = await provider.SearchAsync(criteria);

        Assert.All(flights, f => Assert.StartsWith("GA-", f.FlightNumber));
    }

    [Fact]
    public async Task SearchAsync_AllFlights_ArrivalAfterDeparture()
    {
        var provider = new GlobalAirProvider();
        var criteria = new FlightSearchCriteria(Delhi, London, new DateOnly(2025, 8, 1), 1, "Economy");

        var flights = await provider.SearchAsync(criteria);

        Assert.All(flights, f => Assert.True(f.ArrivalTime > f.DepartureTime));
    }

    [Fact]
    public async Task SearchAsync_DomesticRoute_HasLowerBaseFareThanInternational()
    {
        var provider = new GlobalAirProvider();
        var domestic = new FlightSearchCriteria(Delhi, Mumbai, new DateOnly(2025, 8, 1), 1, "Economy");
        var international = new FlightSearchCriteria(Delhi, London, new DateOnly(2025, 8, 1), 1, "Economy");

        var domesticFlights = await provider.SearchAsync(domestic);
        var internationalFlights = await provider.SearchAsync(international);

        Assert.True(domesticFlights.First().BaseFare < internationalFlights.First().BaseFare);
    }

    [Fact]
    public async Task SearchAsync_BusinessClass_HasHigherBaseFareThanEconomy()
    {
        var provider = new GlobalAirProvider();
        var economy = new FlightSearchCriteria(Delhi, London, new DateOnly(2025, 8, 1), 1, "Economy");
        var business = new FlightSearchCriteria(Delhi, London, new DateOnly(2025, 8, 1), 1, "Business");

        var economyFlights = await provider.SearchAsync(economy);
        var businessFlights = await provider.SearchAsync(business);

        Assert.True(businessFlights.First().BaseFare > economyFlights.First().BaseFare);
    }

    [Fact]
    public async Task SearchAsync_RespectsProviderName()
    {
        var provider = new GlobalAirProvider();
        Assert.Equal("GlobalAir", provider.ProviderName);
    }

    [Fact]
    public async Task SearchAsync_CancellationRequested_ThrowsOperationCancelled()
    {
        var provider = new GlobalAirProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var criteria = new FlightSearchCriteria(Delhi, London, new DateOnly(2025, 8, 1), 1, "Economy");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.SearchAsync(criteria, cts.Token));
    }
}
