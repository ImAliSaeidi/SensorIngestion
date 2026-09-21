using SensorIngestion.Application.Abstractions.Rules.Evaluation;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation.Operators;

public class BetweenOperatorStrategy : IRuleOperatorStrategy
{
    public RuleOperator Operator { get; } = RuleOperator.Create(RuleOperatorNames.Between);

    public OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var lowerBoundParameter = parameters.SingleOrDefault(x => x.Name == RuleParameterNames.LowerBound)
           ?? throw new InvalidOperationException("The Between operator requires a 'LowerBound' parameter.");

        var upperBoundParameter = parameters.SingleOrDefault(x => x.Name == RuleParameterNames.UpperBound)
         ?? throw new InvalidOperationException("The Between operator requires a 'UpperBound' parameter.");

        if (value >= lowerBoundParameter.Value && value <= upperBoundParameter.Value)
            return OperatorEvaluationResult.Satisfied();

        var explanation = $"Between requires value {value} to be between {lowerBoundParameter.Value} and {upperBoundParameter.Value}.";
        return OperatorEvaluationResult.Violated(explanation);
    }
}
