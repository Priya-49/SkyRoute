using SkyRoute.Domain.Entities;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Tests.Domain;

public sealed class BookingTests
{
    [Fact]
    public void Constructor_CreatesBooking_WhenInputsAreValid()
    {
        var booking = CreateBooking(2, 368.00m, 736.00m);

        Assert.Equal(2, booking.Passengers);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void Constructor_Throws_WhenPassengerCountIsOutsideRange()
    {
        var action = () => CreateBooking(0, 368.00m, 0.00m);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_Throws_WhenTotalPriceDoesNotMatchPassengerCount()
    {
        var action = () => CreateBooking(2, 368.00m, 700.00m);

        Assert.Throws<ArgumentException>(action);
    }

    private static Booking CreateBooking(int passengers, decimal pricePerPassenger, decimal totalPrice) =>
        new(
            Guid.NewGuid(),
            new BookingReference("SKY-A3B7X2K"),
            "GlobalAir",
            "GA-4821",
            "JFK",
            "LHR",
            new DateTime(2026, 8, 15, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 8, 15, 20, 0, 0, DateTimeKind.Utc),
            "Economy",
            passengers,
            "Jane Doe",
            "jane.doe@example.com",
            "Passport",
            "P12345678",
            pricePerPassenger,
            totalPrice,
            new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            BookingStatus.Confirmed);
}
