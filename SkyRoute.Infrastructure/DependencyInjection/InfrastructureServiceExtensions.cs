using Microsoft.Extensions.DependencyInjection;
using SkyRoute.Application.Caching;
using SkyRoute.Domain.Interfaces;
using SkyRoute.Infrastructure.Caching;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IFlightProvider, GlobalAirProvider>();
        services.AddScoped<IFlightProvider, BudgetWingsProvider>();
        services.AddScoped<IPricingStrategy, GlobalAirPricingStrategy>();
        services.AddScoped<IPricingStrategy, BudgetWingsPricingStrategy>();
        services.AddSingleton<IFlightSearchCache, FlightSearchCache>();

        return services;
    }
}
