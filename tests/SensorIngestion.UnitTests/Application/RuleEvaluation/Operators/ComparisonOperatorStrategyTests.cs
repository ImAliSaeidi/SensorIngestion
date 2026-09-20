using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Application.Rules.Evaluation.Operators;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Application.RuleEvaluation.Operators;

public sealed class ComparisonOperatorStrategyTests
{
    [Fact]
    public void Strategies_ShouldExposeStableOperatorNames()
    {
        var strategies = new IRuleOperatorStrategy[]
        {
            new GreaterThanOperatorStrategy(),
            new GreaterThanOrEqualOperatorStrategy(),
            new LessThanOperatorStrategy(),
            new LessThanOrEqualOperatorStrategy(),
            new EqualOperatorStrategy(),
            new BetweenOperatorStrategy()
        };

        Assert.Equal(["GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Equal", "Between"], strategies.Select(x => x.Operator.Value));
    }

    [Theory]
    [InlineData(81, true)]
    [InlineData(80, false)]
    [InlineData(79, false)]
    public void GreaterThan_Evaluate_ShouldUseExclusiveBoundary(double value, bool expectedSatisfied)
    {
        var strategy = new GreaterThanOperatorStrategy();

        var result = strategy.Evaluate(value, [RuleParameter.Create(RuleParameterNames.Threshold, 80)]);

        Assert.Equal(expectedSatisfied, result.IsSatisfied);
    }

    [Theory]
    [InlineData(81, true)]
    [InlineData(80, true)]
    [InlineData(79, false)]
    public void GreaterThanOrEqual_Evaluate_ShouldUseInclusiveBoundary(double value, bool expectedSatisfied)
    {
        var strategy = new GreaterThanOrEqualOperatorStrategy();

        var result = strategy.Evaluate(value, [RuleParameter.Create(RuleParameterNames.Threshold, 80)]);

        Assert.Equal(expectedSatisfied, result.IsSatisfied);
    }

    [Theory]
    [InlineData(79, true)]
    [InlineData(80, false)]
    [InlineData(81, false)]
    public void LessThan_Evaluate_ShouldUseExclusiveBoundary(double value, bool expectedSatisfied)
    {
        var strategy = new LessThanOperatorStrategy();

        var result = strategy.Evaluate(value, [RuleParameter.Create(RuleParameterNames.Threshold, 80)]);

        Assert.Equal(expectedSatisfied, result.IsSatisfied);
    }

    [Theory]
    [InlineData(79, true)]
    [InlineData(80, true)]
    [InlineData(81, false)]
    public void LessThanOrEqual_Evaluate_ShouldUseInclusiveBoundary(double value, bool expectedSatisfied)
    {
        var strategy = new LessThanOrEqualOperatorStrategy();

        var result = strategy.Evaluate(value, [RuleParameter.Create(RuleParameterNames.Threshold, 80)]);

        Assert.Equal(expectedSatisfied, result.IsSatisfied);
    }

    [Theory]
    [InlineData(80, true)]
    [InlineData(80.0001, false)]
    [InlineData(79.9999, false)]
    public void Equal_Evaluate_ShouldUseExactNumericEquality(double value, bool expectedSatisfied)
    {
        var strategy = new EqualOperatorStrategy();

        var result = strategy.Evaluate(value, [RuleParameter.Create(RuleParameterNames.Threshold, 80)]);

        Assert.Equal(expectedSatisfied, result.IsSatisfied);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(15, true)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void Between_Evaluate_ShouldIncludeBothBoundaries(double value, bool expectedSatisfied)
    {
        var strategy = new BetweenOperatorStrategy();

        var result = strategy.Evaluate(value, [RuleParameter.Create(RuleParameterNames.LowerBound, 10), RuleParameter.Create(RuleParameterNames.UpperBound, 20)]);

        Assert.Equal(expectedSatisfied, result.IsSatisfied);
    }

    [Fact]
    public void Evaluate_WhenRuleIsNotSatisfied_ShouldProvideMeaningfulExplanation()
    {
        var strategy = new GreaterThanOperatorStrategy();

        var result = strategy.Evaluate(75, [RuleParameter.Create(RuleParameterNames.Threshold, 80)]);

        Assert.False(result.IsSatisfied);
        Assert.False(string.IsNullOrWhiteSpace(result.Explanation));
        Assert.Contains("GreaterThan", result.Explanation, StringComparison.Ordinal);
        Assert.Contains("75", result.Explanation, StringComparison.Ordinal);
        Assert.Contains("80", result.Explanation, StringComparison.Ordinal);
    }
}
