using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.API;

public sealed class AuthControllerRegisterEndpointTests : IClassFixture<AuthControllerRegisterEndpointTests.AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthControllerRegisterEndpointTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ReturnsOk_WithUserDetails()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "new.user@example.com",
            password = "P@ssw0rd123!"
        });

        var payload = await response.Content.ReadFromJsonAsync<RegisterResult>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("new.user@example.com", payload!.Email);
        Assert.Equal("Registration successful.", payload.Message);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_ForDuplicateEmail()
    {
        using var client = _factory.CreateClient();
        var request = new
        {
            email = "duplicate.user@example.com",
            password = "P@ssw0rd123!"
        };

        await client.PostAsJsonAsync("/api/auth/register", request);
        var duplicateResponse = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
    }

    private sealed class RegisterResult
    {
        public string Email { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }

    public sealed class AuthApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"SkyRouteAuthRegisterTests-{Guid.NewGuid()}";

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
