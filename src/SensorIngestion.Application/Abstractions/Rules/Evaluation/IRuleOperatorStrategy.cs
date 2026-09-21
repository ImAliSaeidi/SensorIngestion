using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Abstractions.Rules.Evaluation;

public interface IRuleOperatorStrategy
{
    RuleOperator Operator { get; }

    OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters);
}
