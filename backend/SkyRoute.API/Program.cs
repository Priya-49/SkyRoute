using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Caching;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;
using SkyRoute.Application.Flights;
using FluentValidation;
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
builder.Services.AddControllers();
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
// Flight providers — adding a new carrier requires only a new class + one registration here.
builder.Services.AddScoped<IFlightProvider, GlobalAirProvider>();
builder.Services.AddScoped<IFlightProvider, BudgetWingsProvider>();

// Pricing strategies — each strategy is resolved by ProviderName at search time.
builder.Services.AddSingleton<SkyRoute.Application.Interfaces.IPricingStrategy, GlobalAirPricingStrategy>();
builder.Services.AddSingleton<SkyRoute.Application.Interfaces.IPricingStrategy, BudgetWingsPricingStrategy>();
builder.Services.AddSingleton<IFlightSearchCache, FlightSearchCache>();
builder.Services.AddScoped<SearchFlightsUseCase>();
builder.Services.AddScoped<IValidator<SearchFlightsQuery>, SearchFlightsQueryValidator>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCors(CorsPolicyName);

app.MapControllers();
app.MapGet("/", () => Results.Ok("SkyRoute API is running."));
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
