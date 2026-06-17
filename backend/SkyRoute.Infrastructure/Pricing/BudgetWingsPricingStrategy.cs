using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

/// <summary>
/// BudgetWings pricing: 10 % discount on provider base fare, with a $29.99 floor.
/// </summary>
public sealed class BudgetWingsPricingStrategy : IPricingStrategy
{
    private const decimal MinimumFare = 29.99m;

    public string ProviderName => "BudgetWings";

    public decimal Calculate(decimal baseFare) => Math.Max(Math.Round(baseFare * 0.90m, 2), MinimumFare);
}
