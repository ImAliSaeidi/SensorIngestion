using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Abstractions.Rules.Evaluation.Stateful;

public interface IStatefulRuleEvaluator
{
    RuleOperator Operator { get; }

    StatefulRuleEvaluationResult Evaluate(IReadOnlyCollection<ReadingStream> streams, Rule rule);
}
