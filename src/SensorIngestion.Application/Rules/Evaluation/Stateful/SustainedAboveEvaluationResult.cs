namespace SensorIngestion.Application.Rules.Evaluation.Stateful;

public sealed class SustainedAboveEvaluationResult(IReadOnlyList<ReadingRuleEvaluationDecision> decisions, IReadOnlyList<SustainedEpisode> episodes)
{
    public IReadOnlyList<ReadingRuleEvaluationDecision> Decisions { get; } = decisions.ToList().AsReadOnly();

    public IReadOnlyList<SustainedEpisode> Episodes { get; } = episodes.ToList().AsReadOnly();
}