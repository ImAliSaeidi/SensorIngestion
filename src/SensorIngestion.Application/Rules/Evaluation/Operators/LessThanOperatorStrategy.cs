using SensorIngestion.Application.Abstractions.Rules.Evaluation;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation.Operators;

public class LessThanOperatorStrategy : IRuleOperatorStrategy
{
    public RuleOperator Operator { get; } = RuleOperator.Create(RuleOperatorNames.LessThan);

    public OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var thresholdParameter = parameters.SingleOrDefault(x => x.Name == RuleParameterNames.Threshold)
           ?? throw new InvalidOperationException("The LessThan operator requires a 'threshold' parameter.");

        var threshold = thresholdParameter.Value;

        if (value < threshold)
            return OperatorEvaluationResult.Satisfied();

        var explanation = $"LessThan requires value {value} to be less than threshold {threshold}.";
        return OperatorEvaluationResult.Violated(explanation);
    }
}
