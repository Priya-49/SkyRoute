namespace SkyRoute.Domain.Interfaces;

public interface IPricingStrategy
{
    string ProviderName { get; }

    decimal Calculate(decimal baseFare);
}
