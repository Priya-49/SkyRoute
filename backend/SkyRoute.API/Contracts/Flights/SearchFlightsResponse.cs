namespace SkyRoute.API.Contracts.Flights;

public sealed class SearchFlightsResponse
{
    public List<SearchFlightResultResponse> Results { get; set; } = new();
}
