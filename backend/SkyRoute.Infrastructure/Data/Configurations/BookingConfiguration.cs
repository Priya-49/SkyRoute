using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Infrastructure.Data.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.ReferenceCode)
            .IsRequired()
            .HasMaxLength(12)
            .HasConversion(
                reference => reference.Value,
                value => new BookingReference(value));

        builder.HasIndex(b => b.ReferenceCode).IsUnique();

        builder.Property<Guid?>("FlightId");
        builder.HasIndex("FlightId");

        builder.Property(b => b.Provider).IsRequired().HasMaxLength(50);
        builder.Property(b => b.FlightNumber).IsRequired().HasMaxLength(20);
        builder.Property(b => b.Origin).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(b => b.Destination).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(b => b.DepartureTime).IsRequired();
        builder.Property(b => b.ArrivalTime).IsRequired();
        builder.Property(b => b.CabinClass).IsRequired().HasMaxLength(20);
        builder.Property(b => b.Passengers).IsRequired();
        builder.Property(b => b.PassengerName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Email).IsRequired().HasMaxLength(320);
        builder.Property(b => b.DocumentType).IsRequired().HasMaxLength(20);
        builder.Property(b => b.DocumentNumber).IsRequired().HasMaxLength(50);
        builder.Property(b => b.PricePerPassenger).HasColumnType("decimal(10,2)");
        builder.Property(b => b.TotalPrice).HasColumnType("decimal(10,2)");
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
