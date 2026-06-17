using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Search([FromBody] SearchFlightsQuery query, CancellationToken cancellationToken)
    {
        var results = await _searchFlightsUseCase.ExecuteAsync(query, cancellationToken);
        return Ok(new { results });
    }
}
