using System.ComponentModel.DataAnnotations;
using SkyRoute.Application.Bookings;

namespace SkyRoute.Tests.Application;

public sealed class CreateBookingCommandDataAnnotationsTests
{
    [Fact]
    public void Validate_ReturnsNoErrors_ForValidCommand()
    {
        var command = BuildValidCommand();

        var errors = Validate(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ReturnsErrors_ForInvalidFields()
    {
        var command = BuildValidCommand();
        command.Origin = "JF";
        command.Passengers = 0;
        command.Email = "invalid";
        command.DocumentType = "IdCard";

        var errors = Validate(command);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateBookingCommand.Origin)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateBookingCommand.Passengers)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateBookingCommand.Email)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateBookingCommand.DocumentType)));
    }

    private static CreateBookingCommand BuildValidCommand() =>
        new()
        {
            FlightId = Guid.NewGuid(),
            Origin = "JFK",
            Destination = "LHR",
            Passengers = 2,
            PassengerName = "Jane Doe",
            Email = "jane.doe@example.com",
            DocumentType = "Passport",
            DocumentNumber = "P1234567"
        };

    private static IReadOnlyCollection<ValidationResult> Validate(CreateBookingCommand command)
    {
        var context = new ValidationContext(command);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(command, context, results, validateAllProperties: true);
        return results;
    }
}
