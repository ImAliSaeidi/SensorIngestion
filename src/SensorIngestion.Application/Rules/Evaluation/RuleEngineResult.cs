using SensorIngestion.Application.Rules.Evaluation.Stateful;

namespace SensorIngestion.Application.Rules.Evaluation;

public sealed class RuleEngineResult(IReadOnlyList<ReadingRuleEvaluationDecision> decisions, IReadOnlyList<RuleViolationEpisode> episodes)
{
    public IReadOnlyList<ReadingRuleEvaluationDecision> Decisions { get; } = decisions.ToList().AsReadOnly();

    public IReadOnlyList<RuleViolationEpisode> Episodes { get; } = episodes.ToList().AsReadOnly();
}
