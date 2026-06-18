using System.Net;
using System.Net.Http.Headers;
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
    public async Task Create_ReturnsUnauthorized_WhenNoJwtIsProvided()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/bookings", new
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
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenValidJwtIsProvided()
    {
        using var client = _factory.CreateClient();
        var token = await RegisterAndGetAccessTokenAsync(client, "booker@example.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var firstFlight = await SearchFirstFlightAsync(client, "JFK", "LHR");
        var request = BuildBookingRequest(firstFlight, "Passport", "P1234567");
        var response = await client.PostAsJsonAsync("/api/bookings", request);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Matches("^SKY-[A-Z0-9]{7}$", doc.RootElement.GetProperty("referenceCode").GetString()!);
    }

    [Fact]
    public async Task Mine_ReturnsOnlyBookingsForAuthenticatedUser()
    {
        using var firstUserClient = _factory.CreateClient();
        var firstUserToken = await RegisterAndGetAccessTokenAsync(firstUserClient, "first.user@example.com");
        firstUserClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", firstUserToken);

        using var secondUserClient = _factory.CreateClient();
        var secondUserToken = await RegisterAndGetAccessTokenAsync(secondUserClient, "second.user@example.com");
        secondUserClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", secondUserToken);

        var flight1 = await SearchFirstFlightAsync(firstUserClient, "JFK", "LHR");
        var flight2 = await SearchFirstFlightAsync(secondUserClient, "JFK", "LHR");

        await firstUserClient.PostAsJsonAsync("/api/bookings", BuildBookingRequest(flight1, "Passport", "P1234567"));
        await secondUserClient.PostAsJsonAsync("/api/bookings", BuildBookingRequest(flight2, "Passport", "P7654321"));

        var mineResponse = await firstUserClient.GetAsync("/api/bookings/mine");
        var mineJson = await mineResponse.Content.ReadAsStringAsync();
        using var mineDoc = JsonDocument.Parse(mineJson);

        Assert.Equal(HttpStatusCode.OK, mineResponse.StatusCode);
        var bookings = mineDoc.RootElement.GetProperty("bookings");
        Assert.Single(bookings.EnumerateArray());
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client, string email)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "S3cur3P@ssw0rd!",
            firstName = "Jane",
            lastName = "Doe"
        });
        registerResponse.EnsureSuccessStatusCode();

        var json = await registerResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("accessToken").GetString()!;
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
        return document.RootElement.GetProperty("results")[0].Clone();
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
                    options.UseInMemoryDatabase("SkyRouteAuthBookingsTests");
                });
            });
        }
    }
}
