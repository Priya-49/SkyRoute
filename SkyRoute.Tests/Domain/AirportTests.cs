using SkyRoute.Domain.Entities;

namespace SkyRoute.Tests.Domain;

public sealed class AirportTests
{
    [Fact]
    public void Constructor_Normalizes_ValidCodes()
    {
        var airport = new Airport("jfk", "John F. Kennedy International", "New York", "us");

        Assert.Equal("JFK", airport.Code);
        Assert.Equal("US", airport.CountryCode);
    }

    [Fact]
    public void Constructor_Throws_WhenIataCodeLengthIsInvalid()
    {
        var action = () => new Airport("JF", "John F. Kennedy International", "New York", "US");

        Assert.Throws<ArgumentException>(action);
    }
}
