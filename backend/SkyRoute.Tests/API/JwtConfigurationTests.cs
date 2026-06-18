using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SkyRoute.Tests.API;

public sealed class JwtConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public JwtConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void JwtSection_ContainsRequiredValues()
    {
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        Assert.False(string.IsNullOrWhiteSpace(configuration["Jwt:Key"]));
        Assert.False(string.IsNullOrWhiteSpace(configuration["Jwt:Issuer"]));
        Assert.False(string.IsNullOrWhiteSpace(configuration["Jwt:Audience"]));
        Assert.True(int.TryParse(configuration["Jwt:ExpiryMinutes"], out var expiryMinutes));
        Assert.True(expiryMinutes > 0);
    }
}
