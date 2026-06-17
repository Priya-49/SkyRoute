using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Tests.API;

public sealed class ApiConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task CorsPolicy_AllowsConfiguredOrigin()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Origin", "http://localhost:4200");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Contains("http://localhost:4200", origins);
    }

    [Fact]
    public void DependencyInjection_ResolvesRegisteredServices()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        var providers = services.GetServices<IFlightProvider>();
        var pricingStrategies = services.GetServices<SkyRoute.Application.Interfaces.IPricingStrategy>();
        var cache = services.GetService<IFlightSearchCache>();
        var memoryCache = services.GetService<IMemoryCache>();

        Assert.Equal(2, providers.Count());
        Assert.Equal(2, pricingStrategies.Count());
        Assert.NotNull(cache);
        Assert.NotNull(memoryCache);
    }
}
