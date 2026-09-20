using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation.Operators;

public class GreaterThanOrEqualOperatorStrategy : IRuleOperatorStrategy
{
    public RuleOperator Operator { get; } = RuleOperator.Create(RuleOperatorNames.GreaterThanOrEqual);

    public OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var thresholdParameter = parameters.SingleOrDefault(x => x.Name == RuleParameterNames.Threshold)
           ?? throw new InvalidOperationException("The GreaterThanOrEqual operator requires a 'threshold' parameter.");

        var threshold = thresholdParameter.Value;

        if (value >= threshold)
            return OperatorEvaluationResult.Satisfied();

        var explanation = $"GreaterThanOrEqual requires value {value} to be greater than or equal to threshold {threshold}.";
        return OperatorEvaluationResult.Violated(explanation);
    }
}
