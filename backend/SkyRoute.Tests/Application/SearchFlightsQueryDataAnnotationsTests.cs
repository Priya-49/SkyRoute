using System.ComponentModel.DataAnnotations;
using SkyRoute.Application.Flights;

namespace SkyRoute.Tests.Application;

public sealed class SearchFlightsQueryDataAnnotationsTests
{
    [Fact]
    public void Validate_ReturnsNoErrors_ForValidQuery()
    {
        var query = new SearchFlightsQuery
        {
            Origin = "JFK",
            Destination = "LHR",
            DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(10)),
            Passengers = 2,
            CabinClass = "Economy"
        };

        var errors = Validate(query);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ReturnsErrors_ForInvalidFields()
    {
        var query = new SearchFlightsQuery
        {
            Origin = "JF",
            Destination = "LHR",
            DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)),
            Passengers = 0,
            CabinClass = "Premium"
        };

        var errors = Validate(query);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SearchFlightsQuery.Origin)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SearchFlightsQuery.Passengers)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(SearchFlightsQuery.CabinClass)));
    }

    private static IReadOnlyCollection<ValidationResult> Validate(SearchFlightsQuery query)
    {
        var context = new ValidationContext(query);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(query, context, results, validateAllProperties: true);
        return results;
    }
}
