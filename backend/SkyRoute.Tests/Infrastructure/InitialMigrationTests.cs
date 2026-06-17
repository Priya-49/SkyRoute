using Microsoft.EntityFrameworkCore;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.Infrastructure;

public sealed class InitialMigrationTests
{
    [Fact]
    public void InitialCreate_Migration_IsDiscoverableFromDbContext()
    {
        var options = new DbContextOptionsBuilder<SkyRouteDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=SkyRouteMigrationDiscoveryTest;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var context = new SkyRouteDbContext(options);
        var migrations = context.Database.GetMigrations().ToArray();

        Assert.Contains(migrations, migration => migration.EndsWith("InitialCreate", StringComparison.Ordinal));
    }
}
