using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

public sealed class PercentageMarkupStrategy : IPricingStrategy
{
    public PercentageMarkupStrategy(string providerName, decimal markupPercentage)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or whitespace.", nameof(providerName));
        }

        ProviderName = providerName.Trim();
        MarkupPercentage = markupPercentage;
    }

    public string ProviderName { get; }

    public decimal MarkupPercentage { get; }

    public decimal Calculate(decimal baseFare)
    {
        var multiplier = 1 + (MarkupPercentage / 100m);
        return decimal.Round(baseFare * multiplier, 2, MidpointRounding.AwayFromZero);
    }
}
