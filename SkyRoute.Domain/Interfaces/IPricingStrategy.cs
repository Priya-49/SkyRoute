namespace SkyRoute.Domain.Interfaces;

/// <summary>
/// Defines pricing behavior for a specific flight provider.
/// </summary>
public interface IPricingStrategy
{
    /// <summary>
    /// Gets the provider name this strategy applies to.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Calculates the per-passenger price from a provider base fare.
    /// </summary>
    /// <param name="baseFare">Base fare from the provider.</param>
    /// <returns>Calculated price per passenger.</returns>
    decimal Calculate(decimal baseFare);
}
