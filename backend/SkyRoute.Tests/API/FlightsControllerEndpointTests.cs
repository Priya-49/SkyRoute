using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    [Fact]
    public async Task Search_ReturnsContractShape_WithFormattedPriceStrings()
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
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("results", out var results));
        Assert.Equal(JsonValueKind.Array, results.ValueKind);
        Assert.True(results.GetArrayLength() > 0);

        var first = results[0];
        Assert.True(first.TryGetProperty("flightId", out _));
        Assert.True(first.TryGetProperty("provider", out _));
        Assert.True(first.TryGetProperty("flightNumber", out _));
        Assert.True(first.TryGetProperty("origin", out _));
        Assert.True(first.TryGetProperty("destination", out _));
        Assert.True(first.TryGetProperty("departureTime", out _));
        Assert.True(first.TryGetProperty("arrivalTime", out _));
        Assert.True(first.TryGetProperty("durationMinutes", out _));
        Assert.True(first.TryGetProperty("cabinClass", out _));
        Assert.True(first.TryGetProperty("pricePerPassenger", out var ppp));
        Assert.True(first.TryGetProperty("totalPrice", out var total));
        Assert.Equal(JsonValueKind.String, ppp.ValueKind);
        Assert.Equal(JsonValueKind.String, total.ValueKind);
        Assert.Matches(@"^\d+\.\d{2}$", ppp.GetString()!);
        Assert.Matches(@"^\d+\.\d{2}$", total.GetString()!);
    }

    [Fact]
    public async Task Search_ReturnsBadRequest_ForInvalidRequest()
    {
        using var client = _factory.CreateClient();
        var request = new
        {
            origin = "BAD",
            destination = "LHR",
            departureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(15)),
            passengers = 2,
            cabinClass = "Economy"
        };

        var response = await client.PostAsJsonAsync("/api/flights/search", request);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Origin", out _));
    }

    [Fact]
    public async Task Search_ReturnsBadRequest_ForInvalidDateFormat()
    {
        using var client = _factory.CreateClient();
        const string json = """
                            {
                              "origin": "JFK",
                              "destination": "LHR",
                              "departureDate": "15-08-2026",
                              "passengers": 2,
                              "cabinClass": "Economy"
                            }
                            """;
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/flights/search", content);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
