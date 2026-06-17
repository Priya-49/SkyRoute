using SkyRoute.Application.Flights;

namespace SkyRoute.Tests.Application;

public sealed class SearchFlightsQueryValidatorTests
{
    private readonly SearchFlightsQueryValidator _validator = new();

    [Fact]
    public void Validate_ReturnsValid_ForCompliantQuery()
    {
        var query = BuildValidQuery();

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReturnsError_WhenOriginAndDestinationMatch()
    {
        var query = BuildValidQuery();
        query.Destination = "JFK";

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("Origin and destination must be different airports."));
    }

    [Fact]
    public void Validate_ReturnsError_WhenDepartureDateOutsideAllowedWindow()
    {
        var query = BuildValidQuery();
        query.DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(366));

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(SearchFlightsQuery.DepartureDate));
    }

    private static SearchFlightsQuery BuildValidQuery() =>
        new()
        {
            Origin = "JFK",
            Destination = "LHR",
            DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(10)),
            Passengers = 2,
            CabinClass = "Economy"
        };
}
