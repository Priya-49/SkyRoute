using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

/// <summary>
/// GlobalAir pricing: 15 % markup on provider base fare.
/// </summary>
public sealed class GlobalAirPricingStrategy : IPricingStrategy
{
    public string ProviderName => "GlobalAir";

    public decimal Calculate(decimal baseFare) => Math.Round(baseFare * 1.15m, 2);
}
