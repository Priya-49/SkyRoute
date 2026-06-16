using FluentAssertions;
using SkyRoute.Infrastructure.Pricing;

namespace SkyRoute.Tests.Pricing;

public sealed class GlobalAirPricingStrategyTests
{
    [Theory]
    [InlineData(320.00, 368.00)]
    [InlineData(101.00, 116.15)]
    [InlineData(0.01, 0.01)]
    public void Calculate_ReturnsExpectedPrice(decimal baseFare, decimal expected)
    {
        var strategy = new GlobalAirPricingStrategy();

        var actual = strategy.Calculate(baseFare);

        actual.Should().Be(expected);
    }
}
