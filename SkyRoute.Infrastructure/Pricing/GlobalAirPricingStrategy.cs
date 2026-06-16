using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

public sealed class GlobalAirPricingStrategy : IPricingStrategy
{
    public string ProviderName => "GlobalAir";

    public decimal Calculate(decimal baseFare) =>
        Math.Round(baseFare * 1.15m, 2, MidpointRounding.AwayFromZero);
}
