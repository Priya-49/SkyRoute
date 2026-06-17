using System.ComponentModel.DataAnnotations;

namespace SkyRoute.Application.Flights;

public sealed class SearchFlightsQuery
{
    [Required]
    [RegularExpression("^[A-Z]{3}$")]
    public string Origin { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[A-Z]{3}$")]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public DateOnly DepartureDate { get; set; }

    [Range(1, 9)]
    public int Passengers { get; set; }

    [Required]
    [RegularExpression("^(Economy|Business|FirstClass)$")]
    public string CabinClass { get; set; } = string.Empty;
}
