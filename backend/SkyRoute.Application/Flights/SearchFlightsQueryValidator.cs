using FluentValidation;
using SkyRoute.Application.Data;

namespace SkyRoute.Application.Flights;

public sealed class SearchFlightsQueryValidator : AbstractValidator<SearchFlightsQuery>
{
    private static readonly HashSet<string> AllowedCabinClasses = new(StringComparer.Ordinal)
    {
        "Economy",
        "Business",
        "FirstClass"
    };

    public SearchFlightsQueryValidator()
    {
        RuleFor(x => x.Origin)
            .NotEmpty()
            .Length(3)
            .Must(AirportRegistry.IsKnownCode)
            .WithMessage("Origin airport code is not recognised.");

        RuleFor(x => x.Destination)
            .NotEmpty()
            .Length(3)
            .Must(AirportRegistry.IsKnownCode)
            .WithMessage("Destination airport code is not recognised.");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.Origin, x.Destination, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Origin and destination must be different airports.");

        RuleFor(x => x.DepartureDate)
            .Must(date => date >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Departure date must be today or in the future.")
            .Must(date => date <= DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(365)))
            .WithMessage("Departure date must be within 365 days from today.");

        RuleFor(x => x.Passengers)
            .InclusiveBetween(1, 9);

        RuleFor(x => x.CabinClass)
            .NotEmpty()
            .Must(value => AllowedCabinClasses.Contains(value))
            .WithMessage("Cabin class must be Economy, Business, or FirstClass.");
    }
}
