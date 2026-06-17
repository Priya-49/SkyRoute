using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.Infrastructure;

public sealed class SkyRouteDbContextTests
{
    [Fact]
    public void Bookings_DbSetProperty_IsDeclaredWithBookingType()
    {
        var bookingsProperty = typeof(SkyRouteDbContext).GetProperty(nameof(SkyRouteDbContext.Bookings));

        Assert.NotNull(bookingsProperty);
        Assert.Equal(typeof(Microsoft.EntityFrameworkCore.DbSet<SkyRoute.Domain.Entities.Booking>), bookingsProperty!.PropertyType);
    }
}
