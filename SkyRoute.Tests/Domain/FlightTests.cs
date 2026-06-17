using SkyRoute.Domain.Entities;

namespace SkyRoute.Tests.Domain;

public sealed class FlightTests
{
    [Fact]
    public void Constructor_CreatesFlight_WhenInputsAreValid()
    {
        var origin = new Airport("JFK", "John F. Kennedy International", "New York", "US");
        var destination = new Airport("LHR", "Heathrow", "London", "GB");

        var flight = new Flight(
            Guid.NewGuid(),
            "GA-4821",
            "GlobalAir",
            origin,
            destination,
            new DateTime(2026, 8, 15, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 15, 20, 0, 0, DateTimeKind.Utc),
            "Economy",
            320.00m);

        Assert.Equal("GlobalAir", flight.Provider);
        Assert.Equal(320.00m, flight.BaseFare);
    }

    [Fact]
    public void Constructor_Throws_WhenOriginAndDestinationAreSame()
    {
        var airport = new Airport("JFK", "John F. Kennedy International", "New York", "US");

        var action = () => new Flight(
            Guid.NewGuid(),
            "GA-4821",
            "GlobalAir",
            airport,
            airport,
            new DateTime(2026, 8, 15, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 15, 20, 0, 0, DateTimeKind.Utc),
            "Economy",
            320.00m);

        Assert.Throws<ArgumentException>(action);
    }
}
