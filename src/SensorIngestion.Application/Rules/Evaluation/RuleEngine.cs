using SensorIngestion.Application.Abstractions.Rules.Evaluation.Stateful;
using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation;

public sealed class RuleEngine(StatelessRuleEvaluator statelessEvaluator, StatefulRuleEvaluatorRegistry statefulRegistry)
{
    public RuleEngineResult Evaluate(IReadOnlyCollection<SensorReading> readings, IReadOnlyCollection<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentNullException.ThrowIfNull(rules);

        var decisions = new List<ReadingRuleEvaluationDecision>();
        var episodes = new List<RuleViolationEpisode>();
        var statelessRules = new List<Rule>();
        var statefulRules = new List<(Rule Rule, IStatefulRuleEvaluator Evaluator)>();

        foreach (var rule in rules)
        {
            ArgumentNullException.ThrowIfNull(rule);

            if (statefulRegistry.TryResolve(rule.Operator, out var evaluator))
                statefulRules.Add((rule, evaluator!));
            else
                statelessRules.Add(rule);
        }

        foreach (var reading in readings)
        {
            ArgumentNullException.ThrowIfNull(reading);
            var result = statelessEvaluator.Evaluate(reading, statelessRules);
            decisions.AddRange(result.Decisions.Select(decision => new ReadingRuleEvaluationDecision(reading, decision)));
        }

        if (statefulRules.Count > 0)
        {
            var streams = ReadingStreamOrganizer.Organize(readings);

            foreach (var (rule, evaluator) in statefulRules)
            {
                var result = evaluator.Evaluate(streams, rule);
                decisions.AddRange(result.Decisions);
                episodes.AddRange(result.Episodes);
            }
        }

        var violatedReadings = decisions.Where(x => x.Decision.IsViolated).Select(x => x.Reading).ToHashSet();

        foreach (var reading in readings)
            reading.Classify(violatedReadings.Contains(reading));

        return new RuleEngineResult(decisions, episodes);
    }
}
