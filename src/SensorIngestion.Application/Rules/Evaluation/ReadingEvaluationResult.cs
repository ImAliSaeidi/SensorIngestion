namespace SensorIngestion.Application.Rules.Evaluation;

public sealed class ReadingEvaluationResult
{
    public IReadOnlyList<RuleEvaluationDecision> Decisions { get; }

    public IReadOnlyList<RuleEvaluationDecision> Violations { get; }

    public bool IsAcceptable => Violations.Count == 0;

    public ReadingEvaluationResult(IReadOnlyList<RuleEvaluationDecision> decisions, IReadOnlyList<RuleEvaluationDecision> violations)
    {
        Decisions = decisions;
        Violations = violations;
    }
}