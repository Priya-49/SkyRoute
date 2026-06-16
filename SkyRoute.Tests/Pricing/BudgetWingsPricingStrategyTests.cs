using FluentAssertions;
using SkyRoute.Infrastructure.Pricing;

namespace SkyRoute.Tests.Pricing;

public sealed class BudgetWingsPricingStrategyTests
{
    [Theory]
    [InlineData(200.00, 180.00)]
    [InlineData(30.00, 29.99)]
    [InlineData(20.00, 29.99)]
    [InlineData(29.99, 29.99)]
    public void Calculate_ReturnsExpectedPrice(decimal baseFare, decimal expected)
    {
        var strategy = new BudgetWingsPricingStrategy();

        var actual = strategy.Calculate(baseFare);

        actual.Should().Be(expected);
    }
}
