using SkyRoute.Domain.Entities;
using SkyRoute.Domain.ValueObjects;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Tests.Infrastructure;

public sealed class BudgetWingsProviderTests
{
    private static readonly Airport Delhi = new("DEL", "Indira Gandhi International", "Delhi", "IN");
    private static readonly Airport Mumbai = new("BOM", "Chhatrapati Shivaji Maharaj International", "Mumbai", "IN");
    private static readonly Airport Dubai = new("DXB", "Dubai International", "Dubai", "AE");

    [Fact]
    public async Task SearchAsync_ReturnsTwoFlights_ForAnyRoute()
    {
        var provider = new BudgetWingsProvider();
        var criteria = new FlightSearchCriteria(Delhi, Dubai, new DateOnly(2025, 8, 1), 1, "Economy");

        var flights = await provider.SearchAsync(criteria);

        Assert.Equal(2, flights.Count);
    }

    [Fact]
    public async Task SearchAsync_AllFlights_HaveCorrectProviderName()
    {
        var provider = new BudgetWingsProvider();
        var criteria = new FlightSearchCriteria(Delhi, Dubai, new DateOnly(2025, 8, 1), 1, "Economy");

        var flights = await provider.SearchAsync(criteria);

        Assert.All(flights, f => Assert.Equal("BudgetWings", f.Provider));
    }

    [Fact]
    public async Task SearchAsync_AllFlights_HaveBWPrefixedFlightNumbers()
    {
        var provider = new BudgetWingsProvider();
        var criteria = new FlightSearchCriteria(Delhi, Dubai, new DateOnly(2025, 8, 1), 1, "Economy");

        var flights = await provider.SearchAsync(criteria);

        Assert.All(flights, f => Assert.StartsWith("BW-", f.FlightNumber));
    }

    [Fact]
    public async Task SearchAsync_AllFlights_ArrivalAfterDeparture()
    {
        var provider = new BudgetWingsProvider();
        var criteria = new FlightSearchCriteria(Delhi, Dubai, new DateOnly(2025, 8, 1), 1, "Economy");

        var flights = await provider.SearchAsync(criteria);

        Assert.All(flights, f => Assert.True(f.ArrivalTime > f.DepartureTime));
    }

    [Fact]
    public async Task SearchAsync_DomesticRoute_HasLowerBaseFareThanInternational()
    {
        var provider = new BudgetWingsProvider();
        var domestic = new FlightSearchCriteria(Delhi, Mumbai, new DateOnly(2025, 8, 1), 1, "Economy");
        var international = new FlightSearchCriteria(Delhi, Dubai, new DateOnly(2025, 8, 1), 1, "Economy");

        var domesticFlights = await provider.SearchAsync(domestic);
        var internationalFlights = await provider.SearchAsync(international);

        Assert.True(domesticFlights.First().BaseFare < internationalFlights.First().BaseFare);
    }

    [Fact]
    public async Task SearchAsync_BudgetWings_HasLowerBaseFareThanGlobalAir_ForSameRoute()
    {
        var globalAir = new GlobalAirProvider();
        var budgetWings = new BudgetWingsProvider();
        var criteria = new FlightSearchCriteria(Delhi, Dubai, new DateOnly(2025, 8, 1), 1, "Economy");

        var gaFlights = await globalAir.SearchAsync(criteria);
        var bwFlights = await budgetWings.SearchAsync(criteria);

        Assert.True(bwFlights.First().BaseFare < gaFlights.First().BaseFare);
    }

    [Fact]
    public void ProviderName_ReturnsBudgetWings()
    {
        var provider = new BudgetWingsProvider();
        Assert.Equal("BudgetWings", provider.ProviderName);
    }

    [Fact]
    public async Task SearchAsync_CancellationRequested_ThrowsOperationCancelled()
    {
        var provider = new BudgetWingsProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var criteria = new FlightSearchCriteria(Delhi, Dubai, new DateOnly(2025, 8, 1), 1, "Economy");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.SearchAsync(criteria, cts.Token));
    }
}
