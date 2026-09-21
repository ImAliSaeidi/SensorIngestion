using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation;

public sealed class StatelessRuleEvaluator
{
    private readonly RuleOperatorRegistry _registry;

    public StatelessRuleEvaluator(RuleOperatorRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public ReadingEvaluationResult Evaluate(SensorReading reading, IEnumerable<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(rules);

        var decisions = new List<RuleEvaluationDecision>();

        foreach (var rule in rules)
        {
            ArgumentNullException.ThrowIfNull(rule);

            if (!rule.AppliesTo(reading))
                continue;

            var strategy = _registry.Resolve(rule.Operator);
            var operatorResult = strategy.Evaluate(reading.Value, rule.Parameters);
            var isViolated = !operatorResult.IsSatisfied;

            decisions.Add(new RuleEvaluationDecision(rule, isViolated, isViolated ? operatorResult.Explanation : null));
        }

        return new ReadingEvaluationResult(decisions.AsReadOnly());
    }
}
