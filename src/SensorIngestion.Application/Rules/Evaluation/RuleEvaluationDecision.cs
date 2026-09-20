using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation;

public sealed record RuleEvaluationDecision(string RuleKey, string RuleName, RuleOperator Operator, bool IsViolated, string? Explanation);
