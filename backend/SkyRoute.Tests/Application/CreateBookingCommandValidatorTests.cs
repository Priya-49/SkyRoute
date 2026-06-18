using SkyRoute.Application.Bookings;

namespace SkyRoute.Tests.Application;

public sealed class CreateBookingCommandValidatorTests
{
    private readonly CreateBookingCommandValidator _validator = new();

    [Fact]
    public void Validate_ReturnsValid_ForInternationalRouteWithPassport()
    {
        var command = BuildValidCommand();

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_ReturnsError_WhenInternationalRouteUsesNationalId()
    {
        var command = BuildValidCommand();
        command.DocumentType = "NationalId";
        command.DocumentNumber = "AB12345";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBookingCommand.DocumentType)
            && error.ErrorMessage == "Passport is required for international routes.");
    }

    [Fact]
    public void Validate_ReturnsError_WhenDomesticRouteUsesPassport()
    {
        var command = BuildValidCommand();
        command.Origin = "DEL";
        command.Destination = "BOM";
        command.DocumentType = "Passport";
        command.DocumentNumber = "P1234567";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBookingCommand.DocumentType)
            && error.ErrorMessage == "NationalId is required for domestic routes.");
    }

    [Fact]
    public void Validate_ReturnsError_WhenPassportNumberFormatIsInvalid()
    {
        var command = BuildValidCommand();
        command.DocumentNumber = "P12";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBookingCommand.DocumentNumber));
    }

    [Fact]
    public void Validate_ReturnsError_WhenNationalIdNumberFormatIsInvalid()
    {
        var command = BuildValidCommand();
        command.Origin = "DEL";
        command.Destination = "BOM";
        command.DocumentType = "NationalId";
        command.DocumentNumber = "12@34";

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBookingCommand.DocumentNumber));
    }

    private static CreateBookingCommand BuildValidCommand() =>
        new()
        {
            FlightId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Origin = "JFK",
            Destination = "LHR",
            Passengers = 2,
            PassengerName = "Jane Doe",
            Email = "jane.doe@example.com",
            DocumentType = "Passport",
            DocumentNumber = "P1234567"
        };
}
