using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation.Operators;

public class EqualOperatorStrategy : IRuleOperatorStrategy
{
    public RuleOperator Operator { get; } = RuleOperator.Create(RuleOperatorNames.Equal);

    public OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var thresholdParameter = parameters.SingleOrDefault(x => x.Name == RuleParameterNames.Threshold)
           ?? throw new InvalidOperationException("The Equal operator requires a 'threshold' parameter.");

        var threshold = thresholdParameter.Value;

        if (value == threshold)
            return OperatorEvaluationResult.Satisfied();

        var explanation = $"Equal requires value {value} to be equal to threshold {threshold}.";
        return OperatorEvaluationResult.Violated(explanation);
    }
}
