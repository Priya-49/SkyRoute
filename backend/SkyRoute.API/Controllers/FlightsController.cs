using Microsoft.AspNetCore.Mvc;
using SkyRoute.API.Contracts.Flights;
using SkyRoute.Application.Flights;

namespace SkyRoute.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FlightsController : ControllerBase
{
    private readonly SearchFlightsUseCase _searchFlightsUseCase;

    public FlightsController(SearchFlightsUseCase searchFlightsUseCase)
    {
        _searchFlightsUseCase = searchFlightsUseCase ?? throw new ArgumentNullException(nameof(searchFlightsUseCase));
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchFlightsRequest request, CancellationToken cancellationToken)
    {
        var query = new SearchFlightsQuery
        {
            Origin = request.Origin,
            Destination = request.Destination,
            DepartureDate = request.DepartureDate,
            Passengers = request.Passengers,
            CabinClass = request.CabinClass
        };

        var results = await _searchFlightsUseCase.ExecuteAsync(query, cancellationToken);
        var response = new SearchFlightsResponse
        {
            Results = results
                .Select(result => new SearchFlightResultResponse
                {
                    FlightId = result.FlightId,
                    Provider = result.Provider,
                    FlightNumber = result.FlightNumber,
                    Origin = result.Origin,
                    Destination = result.Destination,
                    DepartureTime = result.DepartureTime,
                    ArrivalTime = result.ArrivalTime,
                    DurationMinutes = result.DurationMinutes,
                    CabinClass = result.CabinClass,
                    PricePerPassenger = result.PricePerPassenger.ToString("0.00"),
                    TotalPrice = result.TotalPrice.ToString("0.00")
                })
                .ToList()
        };

        return Ok(response);
    }
}
