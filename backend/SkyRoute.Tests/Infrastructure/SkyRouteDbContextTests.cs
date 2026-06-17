using Microsoft.EntityFrameworkCore;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.Infrastructure;

public sealed class SkyRouteDbContextTests
{
    [Fact]
    public void Bookings_Entity_IsConfiguredInModel()
    {
        var options = new DbContextOptionsBuilder<SkyRouteDbContext>()
            .UseInMemoryDatabase("skyroute-dbcontext-model-test")
            .Options;

        using var context = new SkyRouteDbContext(options);

        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.ClrType == typeof(SkyRoute.Domain.Entities.Booking));
    }
}
