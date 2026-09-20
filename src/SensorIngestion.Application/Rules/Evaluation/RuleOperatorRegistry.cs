using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation;

public sealed class RuleOperatorRegistry
{
    private readonly IReadOnlyDictionary<string, IRuleOperatorStrategy> _strategies;

    public RuleOperatorRegistry(IEnumerable<IRuleOperatorStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);

        var registeredStrategies = new Dictionary<string, IRuleOperatorStrategy>(StringComparer.Ordinal);

        foreach (var strategy in strategies)
        {
            ArgumentNullException.ThrowIfNull(strategy);

            var operatorName = strategy.Operator.Value;

            if (!registeredStrategies.TryAdd(operatorName, strategy))
                throw new InvalidOperationException($"Operator strategy '{operatorName}' is registered more than once.");
        }

        _strategies = registeredStrategies;
    }

    public IRuleOperatorStrategy Resolve(RuleOperator @operator)
    {
        ArgumentNullException.ThrowIfNull(@operator);

        if (_strategies.TryGetValue(@operator.Value, out var strategy))
            return strategy;

        throw new NotSupportedException($"No strategy is registered for operator '{@operator.Value}'.");
    }
}
