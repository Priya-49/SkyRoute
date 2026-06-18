using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Application.Bookings;

public sealed class GetMyBookingsUseCase
{
    private readonly IBookingRepository _bookingRepository;

    public GetMyBookingsUseCase(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
    }

    public async Task<IReadOnlyList<BookingSummaryDto>> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var bookings = await _bookingRepository.GetByUserIdAsync(userId, cancellationToken);
        return bookings.Select(booking => new BookingSummaryDto
        {
            ReferenceCode = booking.ReferenceCode.Value,
            Provider = booking.Provider,
            FlightNumber = booking.FlightNumber,
            Origin = booking.Origin,
            Destination = booking.Destination,
            DepartureTime = booking.DepartureTime,
            ArrivalTime = booking.ArrivalTime,
            CabinClass = booking.CabinClass,
            Passengers = booking.Passengers,
            PricePerPassenger = booking.PricePerPassenger,
            TotalPrice = booking.TotalPrice,
            CreatedAt = booking.CreatedAt
        }).ToList();
    }
}
