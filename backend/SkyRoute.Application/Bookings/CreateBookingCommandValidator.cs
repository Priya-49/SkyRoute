using FluentValidation;
using SkyRoute.Application.Data;

namespace SkyRoute.Application.Bookings;

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    private static readonly HashSet<string> AllowedDocumentTypes = new(StringComparer.Ordinal)
    {
        "Passport",
        "NationalId"
    };

    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.FlightId)
            .NotEmpty()
            .WithMessage("Flight id is required.");

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

        RuleFor(x => x.Passengers)
            .InclusiveBetween(1, 9);

        RuleFor(x => x.PassengerName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.DocumentType)
            .NotEmpty()
            .Must(type => AllowedDocumentTypes.Contains(type))
            .WithMessage("Document type must be Passport or NationalId.");

        RuleFor(x => x.DocumentNumber)
            .NotEmpty()
            .MaximumLength(50)
            .Must((command, documentNumber) => IsDocumentNumberValid(command.DocumentType, documentNumber))
            .WithMessage("Document number format is invalid for the selected document type.");

        RuleFor(x => x)
            .Custom((command, context) =>
            {
                if (!AirportRegistry.TryGet(command.Origin, out var origin) || origin is null ||
                    !AirportRegistry.TryGet(command.Destination, out var destination) || destination is null)
                {
                    return;
                }

                var isInternational = !string.Equals(origin.CountryCode, destination.CountryCode, StringComparison.Ordinal);
                if (isInternational && !string.Equals(command.DocumentType, "Passport", StringComparison.Ordinal))
                {
                    context.AddFailure(nameof(CreateBookingCommand.DocumentType), "Passport is required for international routes.");
                }
                else if (!isInternational && !string.Equals(command.DocumentType, "NationalId", StringComparison.Ordinal))
                {
                    context.AddFailure(nameof(CreateBookingCommand.DocumentType), "NationalId is required for domestic routes.");
                }
            });
    }

    private static bool IsDocumentNumberValid(string documentType, string documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return false;
        }

        var value = documentNumber.Trim();
        if (!value.All(char.IsLetterOrDigit))
        {
            return false;
        }

        return documentType switch
        {
            "Passport" => value.Length is >= 6 and <= 9,
            "NationalId" => value.Length is >= 5 and <= 20,
            _ => false
        };
    }
}
