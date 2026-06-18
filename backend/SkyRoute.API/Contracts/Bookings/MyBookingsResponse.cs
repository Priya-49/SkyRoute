namespace SkyRoute.API.Contracts.Bookings;

public sealed class MyBookingsResponse
{
    public List<BookingSummaryResponse> Bookings { get; set; } = [];
}
