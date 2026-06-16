using SkyRoute.Domain.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

public sealed class BudgetWingsPricingStrategy : IPricingStrategy
{
    public string ProviderName => "BudgetWings";

    public decimal Calculate(decimal baseFare) => Math.Max(baseFare * 0.90m, 29.99m);
}
