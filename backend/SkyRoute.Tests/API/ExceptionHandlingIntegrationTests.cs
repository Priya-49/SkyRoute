using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SkyRoute.Tests.API;

public sealed class ExceptionHandlingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExceptionHandlingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ValidationException_Returns400WithValidationProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/throw/validation");
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("https://tools.ietf.org/html/rfc7807", problem!.Type);
        Assert.Equal("Validation Failed", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.True(problem.Errors.ContainsKey("origin"));
    }

    [Fact]
    public async Task NotFoundException_Returns404WithProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/throw/not-found");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("https://tools.ietf.org/html/rfc7807", problem!.Type);
        Assert.Equal("Not Found", problem.Title);
        Assert.Equal(404, problem.Status);
        Assert.Equal("The requested resource was not found.", problem.Detail);
    }

    [Fact]
    public async Task UnhandledException_Returns500WithProblemDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/throw/server");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("https://tools.ietf.org/html/rfc7807", problem!.Type);
        Assert.Equal("Internal Server Error", problem.Title);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred. Please try again.", problem.Detail);
    }
}
