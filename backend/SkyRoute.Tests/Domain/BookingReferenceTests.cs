using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Domain;

public sealed class BookingReferenceTests
{
    [Fact]
    public void Constructor_NormalizesLowerCaseReference()
    {
        var bookingReference = new BookingReference("sky-a3b7x2k");

        Assert.Equal("SKY-A3B7X2K", bookingReference.Value);
    }

    [Fact]
    public void Constructor_Throws_WhenReferenceFormatIsInvalid()
    {
        var action = () => new BookingReference("INVALID-123");

        Assert.Throws<ArgumentException>(action);
    }
}
