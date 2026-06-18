using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Caching;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;
using SkyRoute.Application.Flights;
using SkyRoute.Application.Bookings;
using FluentValidation;
using Serilog;
using Microsoft.EntityFrameworkCore;
using SkyRoute.Infrastructure.Data;

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
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddEntityFrameworkStores<SkyRouteDbContext>()
    .AddDefaultTokenProviders();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Missing JWT key configuration.");
        var issuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Missing JWT issuer configuration.");
        var audience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Missing JWT audience configuration.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
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
builder.Services.AddScoped<SearchFlightsUseCase>();
builder.Services.AddScoped<CreateBookingUseCase>();
builder.Services.AddScoped<IValidator<SearchFlightsQuery>, SearchFlightsQueryValidator>();
builder.Services.AddScoped<IValidator<CreateBookingCommand>, CreateBookingCommandValidator>();

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
