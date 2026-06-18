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

public sealed class AuthControllerEndpointTests : IClassFixture<AuthControllerEndpointTests.AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthControllerEndpointTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ReturnsCreated_WithTokenPair()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "auth.register@example.com",
            password = "S3cur3P@ssw0rd!",
            firstName = "Auth",
            lastName = "User"
        });
        var payload = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("accessToken", out _));
        Assert.Equal(900, doc.RootElement.GetProperty("expiresIn").GetInt32());
        Assert.True(doc.RootElement.TryGetProperty("refreshToken", out _));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_ForWrongPassword()
    {
        using var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "auth.login@example.com",
            password = "S3cur3P@ssw0rd!",
            firstName = "Auth",
            lastName = "User"
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "auth.login@example.com",
            password = "WrongPassword1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesTokens_AndRejectsReplay()
    {
        using var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "auth.refresh@example.com",
            password = "S3cur3P@ssw0rd!",
            firstName = "Auth",
            lastName = "User"
        });
        var registerJson = await registerResponse.Content.ReadAsStringAsync();
        using var registerDoc = JsonDocument.Parse(registerJson);
        var oldRefreshToken = registerDoc.RootElement.GetProperty("refreshToken").GetString()!;

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = oldRefreshToken
        });
        var refreshJson = await refreshResponse.Content.ReadAsStringAsync();
        using var refreshDoc = JsonDocument.Parse(refreshJson);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var newRefreshToken = refreshDoc.RootElement.GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(oldRefreshToken, newRefreshToken);

        var replayResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = oldRefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
    }

    [Fact]
    public async Task Revoke_InvalidatesRefreshToken()
    {
        using var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "auth.revoke@example.com",
            password = "S3cur3P@ssw0rd!",
            firstName = "Auth",
            lastName = "User"
        });
        var registerJson = await registerResponse.Content.ReadAsStringAsync();
        using var registerDoc = JsonDocument.Parse(registerJson);
        var refreshToken = registerDoc.RootElement.GetProperty("refreshToken").GetString()!;

        var revokeResponse = await client.PostAsJsonAsync("/api/auth/revoke", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);

        var refreshAfterRevoke = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterRevoke.StatusCode);
    }

    public sealed class AuthApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"SkyRouteAuthTests-{Guid.NewGuid()}";

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
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
