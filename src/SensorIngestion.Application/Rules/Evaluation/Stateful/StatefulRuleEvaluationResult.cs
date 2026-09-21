namespace SensorIngestion.Application.Rules.Evaluation.Stateful;

public sealed class StatefulRuleEvaluationResult(IReadOnlyList<ReadingRuleEvaluationDecision> decisions, IReadOnlyList<RuleViolationEpisode> episodes)
{
    public IReadOnlyList<ReadingRuleEvaluationDecision> Decisions { get; } = decisions.ToList().AsReadOnly();

    public IReadOnlyList<RuleViolationEpisode> Episodes { get; } = episodes.ToList().AsReadOnly();
}
