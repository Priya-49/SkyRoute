namespace SkyRoute.Application.Interfaces;

public interface IPricingStrategy
{
    string ProviderName { get; }

    decimal Calculate(decimal baseFare);
}
