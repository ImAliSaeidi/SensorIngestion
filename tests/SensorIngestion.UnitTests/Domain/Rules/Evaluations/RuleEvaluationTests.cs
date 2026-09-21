using SensorIngestion.Domain.Rules.Evaluations;

namespace SensorIngestion.UnitTests.Domain.Rules.Evaluations;

public sealed class RuleEvaluationTests
{
    [Fact]
    public void Passed_WhenArgumentsAreValid_ShouldCreatePassedEvaluation()
    {
        var evaluatedAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.FromHours(3.5));

        var evaluation = RuleEvaluation.Passed(10, 20, evaluatedAt);

        Assert.Equal(10, evaluation.SensorReadingId);
        Assert.Equal(20, evaluation.RuleId);
        Assert.Equal(RuleEvaluationOutcome.Passed, evaluation.Outcome);
        Assert.Null(evaluation.Reason);
        Assert.Equal(TimeSpan.Zero, evaluation.EvaluatedAt.Offset);
        Assert.Equal(evaluatedAt.UtcDateTime, evaluation.EvaluatedAt.UtcDateTime);
    }

    [Fact]
    public void Violated_WhenArgumentsAreValid_ShouldCreateViolatedEvaluation()
    {
        var evaluation = RuleEvaluation.Violated(10, 20, "  threshold exceeded  ", DateTimeOffset.UtcNow);

        Assert.Equal(RuleEvaluationOutcome.Violated, evaluation.Outcome);
        Assert.Equal("threshold exceeded", evaluation.Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Passed_WhenReadingIdIsNotPositive_ShouldThrowArgumentOutOfRangeException(long readingId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RuleEvaluation.Passed(readingId, 1, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Passed_WhenRuleIdIsNotPositive_ShouldThrowArgumentOutOfRangeException(long ruleId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RuleEvaluation.Passed(1, ruleId, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Violated_WhenReasonIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RuleEvaluation.Violated(1, 1, null!, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Violated_WhenReasonIsWhitespace_ShouldThrowArgumentException(string reason)
    {
        Assert.Throws<ArgumentException>(() => RuleEvaluation.Violated(1, 1, reason, DateTimeOffset.UtcNow));
    }
}
