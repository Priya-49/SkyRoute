using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.API;

public sealed class BookingsControllerEndpointTests : IClassFixture<BookingsControllerEndpointTests.BookingsApiFactory>
{
    private readonly BookingsApiFactory _factory;

    public BookingsControllerEndpointTests(BookingsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithContractShapeForValidRequest()
    {
        using var client = _factory.CreateClient();
        var firstFlight = await SearchFirstFlightAsync(client, "JFK", "LHR");

        var request = BuildBookingRequest(firstFlight, "Passport", "P1234567");
        var response = await client.PostAsJsonAsync("/api/bookings", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("referenceCode", out var referenceCode));
        Assert.Matches("^SKY-[A-Z0-9]{7}$", referenceCode.GetString()!);
        Assert.Equal("Jane Doe", doc.RootElement.GetProperty("passengerName").GetString());
        Assert.Equal(firstFlight.GetProperty("provider").GetString(), doc.RootElement.GetProperty("provider").GetString());
        Assert.Equal(firstFlight.GetProperty("flightNumber").GetString(), doc.RootElement.GetProperty("flightNumber").GetString());
        Assert.Equal(firstFlight.GetProperty("origin").GetString(), doc.RootElement.GetProperty("origin").GetString());
        Assert.Equal(firstFlight.GetProperty("destination").GetString(), doc.RootElement.GetProperty("destination").GetString());
        Assert.Equal(firstFlight.GetProperty("departureTime").GetString(), doc.RootElement.GetProperty("departureTime").GetString());
        Assert.Equal(firstFlight.GetProperty("arrivalTime").GetString(), doc.RootElement.GetProperty("arrivalTime").GetString());
        Assert.Equal(firstFlight.GetProperty("cabinClass").GetString(), doc.RootElement.GetProperty("cabinClass").GetString());
        Assert.Equal(2, doc.RootElement.GetProperty("passengers").GetInt32());
        Assert.Matches(@"^\d+\.\d{2}$", doc.RootElement.GetProperty("pricePerPassenger").GetString()!);
        Assert.Matches(@"^\d+\.\d{2}$", doc.RootElement.GetProperty("totalPrice").GetString()!);
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenFlightIsMissingFromCache()
    {
        using var client = _factory.CreateClient();
        var request = new
        {
            flightId = Guid.NewGuid(),
            provider = "GlobalAir",
            flightNumber = "GA-100",
            origin = "JFK",
            destination = "LHR",
            departureTime = DateTime.UtcNow.AddDays(10),
            arrivalTime = DateTime.UtcNow.AddDays(10).AddHours(7),
            cabinClass = "Economy",
            passengers = 2,
            passengerName = "Jane Doe",
            email = "jane.doe@example.com",
            documentType = "Passport",
            documentNumber = "P1234567"
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Not Found", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal("The selected flight is no longer available. Please search again.", doc.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDocumentTypeDoesNotMatchRouteType()
    {
        using var client = _factory.CreateClient();
        var firstFlight = await SearchFirstFlightAsync(client, "DEL", "BOM");

        var request = BuildBookingRequest(firstFlight, "Passport", "P1234567");
        var response = await client.PostAsJsonAsync("/api/bookings", request);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("DocumentType", out var documentTypeErrors));
        Assert.Contains("NationalId is required for domestic routes.", documentTypeErrors[0].GetString());
    }

    private static async Task<JsonElement> SearchFirstFlightAsync(HttpClient client, string origin, string destination)
    {
        var searchRequest = new
        {
            origin,
            destination,
            departureDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(10)),
            passengers = 2,
            cabinClass = "Economy"
        };

        var searchResponse = await client.PostAsJsonAsync("/api/flights/search", searchRequest);
        searchResponse.EnsureSuccessStatusCode();

        var json = await searchResponse.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);

        var results = document.RootElement.GetProperty("results");
        Assert.True(results.GetArrayLength() > 0);

        return results[0].Clone();
    }

    private static object BuildBookingRequest(JsonElement flight, string documentType, string documentNumber) =>
        new
        {
            flightId = flight.GetProperty("flightId").GetGuid(),
            provider = flight.GetProperty("provider").GetString(),
            flightNumber = flight.GetProperty("flightNumber").GetString(),
            origin = flight.GetProperty("origin").GetString(),
            destination = flight.GetProperty("destination").GetString(),
            departureTime = flight.GetProperty("departureTime").GetString(),
            arrivalTime = flight.GetProperty("arrivalTime").GetString(),
            cabinClass = flight.GetProperty("cabinClass").GetString(),
            passengers = 2,
            passengerName = "Jane Doe",
            email = "jane.doe@example.com",
            documentType,
            documentNumber
        };

    public sealed class BookingsApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<SkyRouteDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<SkyRouteDbContext>>();
                services.RemoveAll<SkyRouteDbContext>();
                services.AddDbContext<SkyRouteDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"SkyRouteBookingsTests-{Guid.NewGuid()}");
                });
            });
        }
    }
}
