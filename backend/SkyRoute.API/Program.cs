using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Caching;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IFlightProvider, MockFlightProvider>();
builder.Services.AddSingleton<SkyRoute.Application.Interfaces.IPricingStrategy>(_ => new PercentageMarkupStrategy("GlobalAir", 15m));
builder.Services.AddSingleton<SkyRoute.Application.Interfaces.IPricingStrategy>(_ => new FixedMarkupStrategy("BudgetWings", 25m));
builder.Services.AddSingleton<IFlightSearchCache, FlightSearchCache>();

var app = builder.Build();

app.UseRouting();

app.MapGet("/", () => Results.Ok("SkyRoute API is running."));

app.Run();

public partial class Program;
