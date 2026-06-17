using System.ComponentModel.DataAnnotations;

namespace SkyRoute.Application.Flights;

public sealed class SearchFlightsQuery
{
    [Required]
    [RegularExpression("^[A-Z]{3}$")]
    public string Origin { get; init; } = string.Empty;

    [Required]
    [RegularExpression("^[A-Z]{3}$")]
    public string Destination { get; init; } = string.Empty;

    [Required]
    public DateOnly DepartureDate { get; init; }

    [Range(1, 9)]
    public int Passengers { get; init; }

    [Required]
    [RegularExpression("^(Economy|Business|FirstClass)$")]
    public string CabinClass { get; init; } = string.Empty;
}
