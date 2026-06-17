using Microsoft.EntityFrameworkCore;
using SkyRoute.Domain.Entities;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.Infrastructure;

public sealed class BookingConfigurationTests
{
    [Fact]
    public void BookingConfiguration_CreatesIndexes_ForReferenceCodeAndFlightId()
    {
        var options = new DbContextOptionsBuilder<SkyRouteDbContext>()
            .UseInMemoryDatabase("booking-configuration-test")
            .Options;

        using var context = new SkyRouteDbContext(options);
        var bookingEntity = context.Model.FindEntityType(typeof(Booking));

        Assert.NotNull(bookingEntity);
        Assert.Contains(bookingEntity!.GetIndexes(), index => index.Properties.Any(property => property.Name == nameof(Booking.ReferenceCode)));
        Assert.Contains(bookingEntity.GetIndexes(), index => index.Properties.Any(property => property.Name == "FlightId"));
    }
}
