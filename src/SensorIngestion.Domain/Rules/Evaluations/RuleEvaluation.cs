using SensorIngestion.Domain.Common;

namespace SensorIngestion.Domain.Rules.Evaluations;

public sealed class RuleEvaluation : Entity
{
    public long SensorReadingId { get; private set; }

    public long RuleId { get; private set; }

    public RuleEvaluationOutcome Outcome { get; private set; }

    public string? Reason { get; private set; }

    public DateTimeOffset EvaluatedAt { get; private set; }

    private RuleEvaluation() { }

    private RuleEvaluation(long sensorReadingId, long ruleId, RuleEvaluationOutcome outcome, string? reason, DateTimeOffset evaluatedAt)
    {
        if (sensorReadingId <= 0)
            throw new ArgumentOutOfRangeException(nameof(sensorReadingId), "rule evaluation sensor reading id is required");

        if (ruleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(ruleId), "rule evaluation rule id is required");

        if (outcome == RuleEvaluationOutcome.Violated && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("violation reason is required", nameof(reason));

        SensorReadingId = sensorReadingId;
        RuleId = ruleId;
        Outcome = outcome;
        Reason = reason?.Trim();
        EvaluatedAt = evaluatedAt.ToUniversalTime();
    }

    public static RuleEvaluation Passed(long sensorReadingId, long ruleId, DateTimeOffset evaluatedAt)
        => new(sensorReadingId, ruleId, RuleEvaluationOutcome.Passed, null, evaluatedAt);

    public static RuleEvaluation Violated(long sensorReadingId, long ruleId, string reason, DateTimeOffset evaluatedAt)
        => new(sensorReadingId, ruleId, RuleEvaluationOutcome.Violated, reason, evaluatedAt);
}
