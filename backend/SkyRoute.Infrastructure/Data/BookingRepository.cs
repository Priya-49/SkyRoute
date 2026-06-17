using Microsoft.EntityFrameworkCore;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Infrastructure.Data;

public sealed class BookingRepository : IBookingRepository
{
    private readonly SkyRouteDbContext _context;

    public BookingRepository(SkyRouteDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task SaveAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        if (booking == null)
        {
            throw new ArgumentNullException(nameof(booking));
        }

        const int maxRetries = 3;
        var delays = new[] { 100, 200, 400 }; // milliseconds

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException ex) when (
                ex.InnerException?.Message.Contains("UQ_Bookings_ReferenceCode", StringComparison.OrdinalIgnoreCase) == true
                && attempt < maxRetries - 1)
            {
                // Booking reference collision detected - retry with backoff
                _context.ChangeTracker.Clear();
                await Task.Delay(delays[attempt], cancellationToken);
            }
        }

        // If we reach here after all retries, the collision persists
        throw new InvalidOperationException(
            "Unable to save booking due to reference code collision after multiple retries.");
    }

    public async Task<Booking?> GetByReferenceAsync(BookingReference referenceCode, CancellationToken cancellationToken = default)
    {
        if (referenceCode == null)
        {
            throw new ArgumentNullException(nameof(referenceCode));
        }

        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.ReferenceCode == referenceCode, cancellationToken);
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Booking ID cannot be empty.", nameof(id));
        }

        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
