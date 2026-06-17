using SkyRoute.Infrastructure.Pricing;

namespace SkyRoute.Tests.Infrastructure;

public sealed class PricingStrategiesTests
{
    [Fact]
    public void PercentageMarkupStrategy_CalculatesExpectedPrice_ForPositiveBaseFare()
    {
        var strategy = new PercentageMarkupStrategy("GlobalAir", 15m);

        var result = strategy.Calculate(320m);

        Assert.Equal(368m, result);
    }

    [Fact]
    public void PercentageMarkupStrategy_CalculatesExpectedPrice_ForZeroBaseFare()
    {
        var strategy = new PercentageMarkupStrategy("GlobalAir", 15m);

        var result = strategy.Calculate(0m);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void PercentageMarkupStrategy_CalculatesExpectedPrice_ForNegativeBaseFare()
    {
        var strategy = new PercentageMarkupStrategy("GlobalAir", 15m);

        var result = strategy.Calculate(-100m);

        Assert.Equal(-115m, result);
    }

    [Fact]
    public void PercentageMarkupStrategy_CalculatesExpectedPrice_ForLargeBaseFare()
    {
        var strategy = new PercentageMarkupStrategy("GlobalAir", 15m);

        var result = strategy.Calculate(1_000_000m);

        Assert.Equal(1_150_000m, result);
    }

    [Fact]
    public void FixedMarkupStrategy_CalculatesExpectedPrice_ForPositiveBaseFare()
    {
        var strategy = new FixedMarkupStrategy("BudgetWings", 25m);

        var result = strategy.Calculate(320m);

        Assert.Equal(345m, result);
    }

    [Fact]
    public void FixedMarkupStrategy_CalculatesExpectedPrice_ForZeroBaseFare()
    {
        var strategy = new FixedMarkupStrategy("BudgetWings", 25m);

        var result = strategy.Calculate(0m);

        Assert.Equal(25m, result);
    }

    [Fact]
    public void FixedMarkupStrategy_CalculatesExpectedPrice_ForNegativeBaseFare()
    {
        var strategy = new FixedMarkupStrategy("BudgetWings", 25m);

        var result = strategy.Calculate(-100m);

        Assert.Equal(-75m, result);
    }

    [Fact]
    public void FixedMarkupStrategy_CalculatesExpectedPrice_ForLargeBaseFare()
    {
        var strategy = new FixedMarkupStrategy("BudgetWings", 25m);

        var result = strategy.Calculate(1_000_000m);

        Assert.Equal(1_000_025m, result);
    }
}
