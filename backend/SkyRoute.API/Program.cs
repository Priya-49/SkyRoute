using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Caching;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/skyroute-.log", rollingInterval: RollingInterval.Day);
});

builder.Services.AddRouting();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IFlightProvider, MockFlightProvider>();
builder.Services.AddSingleton<SkyRoute.Application.Interfaces.IPricingStrategy>(_ => new PercentageMarkupStrategy("GlobalAir", 15m));
builder.Services.AddSingleton<SkyRoute.Application.Interfaces.IPricingStrategy>(_ => new FixedMarkupStrategy("BudgetWings", 25m));
builder.Services.AddSingleton<IFlightSearchCache, FlightSearchCache>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseRouting();

app.MapGet("/", () => Results.Ok("SkyRoute API is running."));

app.Run();

public partial class Program;
