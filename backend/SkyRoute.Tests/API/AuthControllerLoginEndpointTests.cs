using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.API;

public sealed class AuthControllerLoginEndpointTests : IClassFixture<AuthControllerLoginEndpointTests.AuthLoginApiFactory>
{
    private readonly AuthLoginApiFactory _factory;

    public AuthControllerLoginEndpointTests(AuthLoginApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ReturnsOk_WithJwtToken_ForValidCredentials()
    {
        using var client = _factory.CreateClient();
        var credentials = new
        {
            email = "login.user@example.com",
            password = "P@ssw0rd123!"
        };

        await client.PostAsJsonAsync("/api/auth/register", credentials);
        var response = await client.PostAsJsonAsync("/api/auth/login", credentials);
        var payload = await response.Content.ReadFromJsonAsync<LoginResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(payload.Token);
        Assert.Equal("skyroute-tests", token.Issuer);
        Assert.Contains("skyroute-tests-client", token.Audiences);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForInvalidCredentials()
    {
        using var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "wrong.password@example.com",
            password = "P@ssw0rd123!"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "wrong.password@example.com",
            password = "WrongPassword1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed class LoginResult
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }

    public sealed class AuthLoginApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"SkyRouteAuthLoginTests-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "skyroute-unit-test-secret-key-with-32chars!",
                    ["Jwt:Issuer"] = "skyroute-tests",
                    ["Jwt:Audience"] = "skyroute-tests-client",
                    ["Jwt:ExpiryMinutes"] = "60"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<SkyRouteDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<SkyRouteDbContext>>();
                services.RemoveAll<SkyRouteDbContext>();
                services.AddDbContext<SkyRouteDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
            });
        }
    }
}
