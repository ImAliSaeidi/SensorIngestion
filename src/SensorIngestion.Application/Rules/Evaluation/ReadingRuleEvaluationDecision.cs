using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Application.Rules.Evaluation;

public sealed record ReadingRuleEvaluationDecision(SensorReading Reading, RuleEvaluationDecision Decision);
