using SensorIngestion.Application.Abstractions.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation.Stateful;

public sealed class StatefulRuleEvaluatorRegistry
{
    private readonly IReadOnlyDictionary<string, IStatefulRuleEvaluator> _evaluators;

    public StatefulRuleEvaluatorRegistry(IEnumerable<IStatefulRuleEvaluator> evaluators)
    {
        ArgumentNullException.ThrowIfNull(evaluators);

        var registeredEvaluators = new Dictionary<string, IStatefulRuleEvaluator>(StringComparer.Ordinal);

        foreach (var evaluator in evaluators)
        {
            ArgumentNullException.ThrowIfNull(evaluator);

            if (!registeredEvaluators.TryAdd(evaluator.Operator.Value, evaluator))
                throw new InvalidOperationException($"Stateful evaluator '{evaluator.Operator.Value}' is registered more than once.");
        }

        _evaluators = registeredEvaluators;
    }

    public bool TryResolve(RuleOperator @operator, out IStatefulRuleEvaluator? evaluator)
    {
        ArgumentNullException.ThrowIfNull(@operator);
        return _evaluators.TryGetValue(@operator.Value, out evaluator);
    }
}
