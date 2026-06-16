using SkyRoute.Domain.Entities;

namespace SkyRoute.Domain.Interfaces;

/// <summary>
/// Defines booking persistence operations.
/// </summary>
public interface IBookingRepository
{
    /// <summary>
    /// Persists a booking record.
    /// </summary>
    /// <param name="booking">Booking to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(Booking booking, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a booking by its reference code.
    /// </summary>
    /// <param name="referenceCode">Booking reference code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matched booking if found, otherwise null.</returns>
    Task<Booking?> GetByReferenceAsync(string referenceCode, CancellationToken cancellationToken = default);
}
