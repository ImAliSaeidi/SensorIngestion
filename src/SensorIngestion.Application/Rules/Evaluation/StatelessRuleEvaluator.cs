using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation;

public sealed class StatelessRuleEvaluator
{
    private readonly RuleOperatorRegistry _registry;

    public StatelessRuleEvaluator(RuleOperatorRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public ReadingEvaluationResult Evaluate(SensorReading reading, IEnumerable<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(reading);
        ArgumentNullException.ThrowIfNull(rules);

        var decisions = new List<RuleEvaluationDecision>();
        var violations = new List<RuleEvaluationDecision>();

        foreach (var rule in rules)
        {
            ArgumentNullException.ThrowIfNull(rule);

            if (!rule.AppliesTo(reading))
                continue;

            var strategy = _registry.Resolve(rule.Operator);
            var operatorResult = strategy.Evaluate(reading.Value, rule.Parameters);
            var isViolated = !operatorResult.IsSatisfied;

            var decision = new RuleEvaluationDecision(
                rule.RuleKey,
                rule.Name,
                rule.Operator,
                isViolated,
                isViolated ? operatorResult.Explanation : null);

            decisions.Add(decision);

            if (isViolated)
                violations.Add(decision);
        }

        reading.Classify(violations.Count > 0);

        return new ReadingEvaluationResult(decisions.AsReadOnly(), violations.AsReadOnly());
    }
}