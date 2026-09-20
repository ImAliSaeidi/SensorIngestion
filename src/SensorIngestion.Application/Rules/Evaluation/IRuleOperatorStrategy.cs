using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation;

public interface IRuleOperatorStrategy
{
    RuleOperator Operator { get; }

    OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters);
}