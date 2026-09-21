namespace SensorIngestion.Application.Rules.Evaluation;

public sealed class ReadingEvaluationResult
{
    public IReadOnlyList<RuleEvaluationDecision> Decisions { get; }

    public IReadOnlyList<RuleEvaluationDecision> Violations => Decisions.Where(x => x.IsViolated).ToArray();

    public bool IsAcceptable => Violations.Count == 0;

    public ReadingEvaluationResult(IReadOnlyList<RuleEvaluationDecision> decisions)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        Decisions = decisions;
    }
}
