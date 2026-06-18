using Microsoft.EntityFrameworkCore;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.Infrastructure;

public sealed class IdentityModelTests
{
    [Fact]
    public void OnModelCreating_MapsApplicationUserToAspNetUsersTable()
    {
        var options = new DbContextOptionsBuilder<SkyRouteDbContext>()
            .UseInMemoryDatabase($"SkyRouteIdentityModelTests-{Guid.NewGuid()}")
            .Options;

        using var context = new SkyRouteDbContext(options);
        var userEntity = context.Model.FindEntityType(typeof(ApplicationUser));

        Assert.NotNull(userEntity);
        Assert.Equal("AspNetUsers", userEntity!.GetTableName());
    }
}
