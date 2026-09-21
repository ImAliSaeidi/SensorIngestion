using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.UnitTests.Domain.Metrics;

public sealed class MetricTests
{
    [Fact]
    public void Create_WhenValueIsValid_ShouldTrimAndNormalizeValue()
    {
        var metric = Metric.Create("  TEMPERATURE  ");

        Assert.Equal("temperature", metric.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenValueIsBlank_ShouldThrowArgumentException(string? value)
    {
        Assert.Throws<ArgumentException>(() => Metric.Create(value!));
    }

    [Fact]
    public void Create_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Metric.Create(null!));
    }

    [Fact]
    public void Equality_WhenNormalizedValuesMatch_ShouldBeEqual()
    {
        var first = Metric.Create("Temperature");
        var second = Metric.Create(" temperature ");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equality_WhenValuesDiffer_ShouldNotBeEqual()
    {
        Assert.NotEqual(Metric.Temperature, Metric.Pressure);
    }

    [Fact]
    public void PredefinedMetrics_ShouldContainExpectedValues()
    {
        Assert.Equal("temperature", Metric.Temperature.Value);
        Assert.Equal("pressure", Metric.Pressure.Value);
        Assert.Equal("vibration", Metric.Vibration.Value);
    }
}
