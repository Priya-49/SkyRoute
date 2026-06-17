using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

public sealed class FixedMarkupStrategy : IPricingStrategy
{
    public FixedMarkupStrategy(string providerName, decimal fixedMarkup)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or whitespace.", nameof(providerName));
        }

        ProviderName = providerName.Trim();
        FixedMarkup = fixedMarkup;
    }

    public string ProviderName { get; }

    public decimal FixedMarkup { get; }

    public decimal Calculate(decimal baseFare) =>
        decimal.Round(baseFare + FixedMarkup, 2, MidpointRounding.AwayFromZero);
}
