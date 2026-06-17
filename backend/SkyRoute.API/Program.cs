using SkyRoute.API.Middleware;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Caching;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "SkyRouteCors";

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/skyroute-.log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddRouting();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddScoped<IFlightProvider, MockFlightProvider>();
builder.Services.AddSingleton<SkyRoute.Application.Interfaces.IPricingStrategy>(_ => new PercentageMarkupStrategy("GlobalAir", 15m));
builder.Services.AddSingleton<SkyRoute.Application.Interfaces.IPricingStrategy>(_ => new FixedMarkupStrategy("BudgetWings", 25m));
builder.Services.AddSingleton<IFlightSearchCache, FlightSearchCache>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRouting();
app.UseCors(CorsPolicyName);

app.MapGet("/", () => Results.Ok("SkyRoute API is running."));
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
