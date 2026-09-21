using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation;

public sealed record RuleEvaluationDecision(Rule Rule, bool IsViolated, string? Explanation)
{
    public string RuleKey => Rule.RuleKey;

    public string RuleName => Rule.Name;

    public RuleOperator Operator => Rule.Operator;
}
