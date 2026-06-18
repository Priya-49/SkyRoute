using Microsoft.EntityFrameworkCore;
using SkyRoute.Domain.Entities;

namespace SkyRoute.Infrastructure.Data;

public sealed class SkyRouteDbContext : DbContext
{
    public SkyRouteDbContext(DbContextOptions<SkyRouteDbContext> options)
        : base(options)
    {
    }

    public DbSet<Booking> Bookings => Set<Booking>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SkyRouteDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
