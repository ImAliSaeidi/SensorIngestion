using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Domain.Rules;

public sealed class RuleOperatorTests
{
    [Fact]
    public void Create_WhenValueIsValid_ShouldTrimValue()
    {
        var operation = RuleOperator.Create("  GreaterThan  ");

        Assert.Equal("GreaterThan", operation.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenValueIsBlank_ShouldThrowArgumentNullException(string? value)
    {
        Assert.Throws<ArgumentNullException>(() => RuleOperator.Create(value!));
    }

    [Fact]
    public void Equality_WhenValuesMatch_ShouldBeEqual()
    {
        Assert.Equal(RuleOperator.Create("GreaterThan"), RuleOperator.Create(" GreaterThan "));
    }
}
