using System.ComponentModel.DataAnnotations;

namespace SkyRoute.Application.Bookings;

public sealed class CreateBookingCommand
{
    [Required]
    public Guid FlightId { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [RegularExpression("^[A-Z]{3}$")]
    public string Origin { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^[A-Z]{3}$")]
    public string Destination { get; set; } = string.Empty;

    [Range(1, 9)]
    public int Passengers { get; set; }

    [Required]
    [MaxLength(200)]
    public string PassengerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Passport|NationalId)$")]
    public string DocumentType { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DocumentNumber { get; set; } = string.Empty;
}
