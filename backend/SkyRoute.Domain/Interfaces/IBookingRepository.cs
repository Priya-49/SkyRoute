using SkyRoute.Domain.Entities;
using SkyRoute.Domain.ValueObjects;

namespace SkyRoute.Domain.Interfaces;

public interface IBookingRepository
{
    Task SaveAsync(Booking booking, CancellationToken cancellationToken = default);

    Task<Booking?> GetByReferenceAsync(BookingReference referenceCode, CancellationToken cancellationToken = default);
}
