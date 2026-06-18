using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Caching;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;
using SkyRoute.Application.Flights;
using SkyRoute.Application.Bookings;
using SkyRoute.Application.Auth;
using FluentValidation;
using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SkyRoute.Infrastructure.Data;
using SkyRoute.Infrastructure.Authentication;
using SkyRoute.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "SkyRouteCors";
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured with at least 32 characters.");
}

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SkyRoute API", Version = "v1" });
});
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<SkyRouteDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SkyRouteDatabase"));
});
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
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<SkyRoute.Application.Interfaces.IPasswordHasher, AspNetPasswordHasher>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<SearchFlightsUseCase>();
builder.Services.AddScoped<CreateBookingUseCase>();
builder.Services.AddScoped<GetMyBookingsUseCase>();
builder.Services.AddScoped<RegisterUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<RefreshTokenUseCase>();
builder.Services.AddScoped<RevokeTokenUseCase>();
builder.Services.AddScoped<IValidator<SearchFlightsQuery>, SearchFlightsQueryValidator>();
builder.Services.AddScoped<IValidator<CreateBookingCommand>, CreateBookingCommandValidator>();
builder.Services.AddScoped<IValidator<RegisterCommand>, RegisterCommandValidator>();
builder.Services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
builder.Services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
builder.Services.AddScoped<IValidator<RevokeTokenCommand>, RevokeTokenCommandValidator>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SkyRoute API v1"));
app.UseRouting();
app.UseCors(CorsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok("SkyRoute API is running."));
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
