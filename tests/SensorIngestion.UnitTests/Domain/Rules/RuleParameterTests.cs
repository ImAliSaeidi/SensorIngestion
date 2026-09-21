using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Domain.Rules;

public sealed class RuleParameterTests
{
    [Fact]
    public void Create_WhenArgumentsAreValid_ShouldTrimNameAndPersistValue()
    {
        var parameter = RuleParameter.Create("  threshold  ", 80);

        Assert.Equal("threshold", parameter.Name);
        Assert.Equal(80, parameter.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameIsBlank_ShouldThrowArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => RuleParameter.Create(name!, 1));
    }

    [Fact]
    public void Create_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RuleParameter.Create(null!, 1));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Create_WhenValueIsNotFinite_ShouldThrowArgumentException(double value)
    {
        Assert.Throws<ArgumentException>(() => RuleParameter.Create("threshold", value));
    }

    [Fact]
    public void Equality_WhenNameAndValueMatch_ShouldBeEqual()
    {
        Assert.Equal(RuleParameter.Create("threshold", 80), RuleParameter.Create("threshold", 80));
    }

    [Fact]
    public void Equality_WhenNameOrValueDiffers_ShouldNotBeEqual()
    {
        var parameter = RuleParameter.Create("threshold", 80);

        Assert.NotEqual(parameter, RuleParameter.Create("minimum", 80));
        Assert.NotEqual(parameter, RuleParameter.Create("threshold", 81));
    }
}
