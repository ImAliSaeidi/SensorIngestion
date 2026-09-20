using SensorIngestion.Application.Rules;
using SensorIngestion.Infrastructure.RuleConfiguration.Models;

namespace SensorIngestion.Infrastructure.RuleConfiguration;

internal static class RuleConfigurationValidator
{
    private static readonly HashSet<string> ComparisonOperators =
    [
        RuleOperatorNames.GreaterThan,
        RuleOperatorNames.GreaterThanOrEqual,
        RuleOperatorNames.LessThan,
        RuleOperatorNames.LessThanOrEqual,
        RuleOperatorNames.Equal
    ];

    public static void Validate(IReadOnlyCollection<RuleJsonDto> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        ValidateDuplicateIds(rules);

        foreach (var rule in rules)
            ValidateRule(rule);
    }

    private static void ValidateDuplicateIds(IEnumerable<RuleJsonDto> rules)
    {
        var duplicateId = rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Id))
            .GroupBy(rule => rule.Id!.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
            throw new InvalidDataException($"Duplicate rule id '{duplicateId.Key}'.");
    }

    private static void ValidateRule(RuleJsonDto rule)
    {
        if (string.IsNullOrWhiteSpace(rule.Id))
            throw new InvalidDataException("rule id is required.");

        if (string.IsNullOrWhiteSpace(rule.Name))
            throw new InvalidDataException($"rule '{rule.Id}' must have a name.");

        if (string.IsNullOrWhiteSpace(rule.Metric))
            throw new InvalidDataException($"rule '{rule.Id}' must have a metric.");

        if (string.IsNullOrWhiteSpace(rule.Operator))
            throw new InvalidDataException($"rule '{rule.Id}' must have an operator.");

        if (rule.Enabled is null)
            throw new InvalidDataException($"rule '{rule.Id}' must define enabled.");

        if (rule.DeviceId is not null && string.IsNullOrWhiteSpace(rule.DeviceId))
            throw new InvalidDataException($"rule '{rule.Id}' contains an invalid deviceId.");

        ValidateOperatorParameters(rule);
    }

    private static void ValidateOperatorParameters(RuleJsonDto rule)
    {
        if (ComparisonOperators.Contains(rule.Operator!))
        {
            RequireFinite(rule.Threshold, rule.Id!, RuleParameterNames.Threshold);
            return;
        }

        if (rule.Operator == RuleOperatorNames.Between)
        {
            RequireFinite(rule.LowerBound, rule.Id!, RuleParameterNames.LowerBound);
            RequireFinite(rule.UpperBound, rule.Id!, RuleParameterNames.UpperBound);

            if (rule.LowerBound >= rule.UpperBound)
                throw new InvalidDataException($"Rule '{rule.Id}' requires lowerBound < upperBound.");

            return;
        }

        if (rule.Operator == RuleOperatorNames.SustainedAbove)
        {
            RequireFinite(rule.Threshold, rule.Id!, RuleParameterNames.Threshold);
            RequireFinite(rule.DurationSeconds, rule.Id!, RuleParameterNames.DurationSeconds);

            if (rule.DurationSeconds <= 0)
                throw new InvalidDataException($"Rule '{rule.Id}' requires a positive durationSeconds.");

            return;
        }

        throw new InvalidDataException($"Rule '{rule.Id}' uses unknown operator '{rule.Operator}'.");
    }

    private static void RequireFinite(double? value, string ruleId, string parameterName)
    {
        if (value is null || !double.IsFinite(value.Value))
            throw new InvalidDataException($"Rule '{ruleId}' requires a finite {parameterName}.");
    }
}
