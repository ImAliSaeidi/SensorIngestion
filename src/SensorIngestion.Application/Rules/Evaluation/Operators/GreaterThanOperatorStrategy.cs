using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation.Operators;

public class GreaterThanOperatorStrategy : IRuleOperatorStrategy
{
    public RuleOperator Operator { get; } = RuleOperator.Create(RuleOperatorNames.GreaterThan);

    public OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var thresholdParameter = parameters.SingleOrDefault(x => x.Name == RuleParameterNames.Threshold)
            ?? throw new InvalidOperationException("The GreaterThan operator requires a 'threshold' parameter.");

        var threshold = thresholdParameter.Value;

        if (value > threshold)
            return OperatorEvaluationResult.Satisfied();

        var explanation = $"GreaterThan requires value {value} to be greater than threshold {threshold}.";
        return OperatorEvaluationResult.Violated(explanation);
    }
}
