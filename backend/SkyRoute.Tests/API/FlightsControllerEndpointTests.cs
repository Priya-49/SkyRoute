using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SkyRoute.Tests.API;

public sealed class FlightsControllerEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FlightsControllerEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_ReturnsOk_ForValidRequest()
    {
        using var client = _factory.CreateClient();
        var request = new
        {
            origin = "JFK",
            destination = "LHR",
            departureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(15)),
            passengers = 2,
            cabinClass = "Economy"
        };

        var response = await client.PostAsJsonAsync("/api/flights/search", request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
