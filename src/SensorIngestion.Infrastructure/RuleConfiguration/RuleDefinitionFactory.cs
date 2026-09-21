using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Rules;
using SensorIngestion.Infrastructure.RuleConfiguration.Models;

namespace SensorIngestion.Infrastructure.RuleConfiguration;

internal static class RuleDefinitionFactory
{
    private static readonly HashSet<string> ComparisonOperators =
    [
        RuleOperatorNames.GreaterThan,
        RuleOperatorNames.GreaterThanOrEqual,
        RuleOperatorNames.LessThan,
        RuleOperatorNames.LessThanOrEqual,
        RuleOperatorNames.Equal
    ];

    public static IReadOnlyList<RuleDefinition> CreateAll(IReadOnlyCollection<RuleJsonDto> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var duplicateId = rules
            .Where(rule => !string.IsNullOrWhiteSpace(rule.Id))
            .GroupBy(rule => rule.Id!.Trim(), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateId is not null)
            throw new InvalidDataException($"Duplicate rule id '{duplicateId.Key}'.");

        return rules.Select(Create).ToList().AsReadOnly();
    }

    private static RuleDefinition Create(RuleJsonDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Id))
            throw new InvalidDataException("rule id is required.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidDataException($"rule '{dto.Id}' must have a name.");

        if (string.IsNullOrWhiteSpace(dto.Metric))
            throw new InvalidDataException($"rule '{dto.Id}' must have a metric.");

        if (string.IsNullOrWhiteSpace(dto.Operator))
            throw new InvalidDataException($"rule '{dto.Id}' must have an operator.");

        if (dto.Enabled is null)
            throw new InvalidDataException($"rule '{dto.Id}' must define enabled.");

        if (dto.DeviceId is not null && string.IsNullOrWhiteSpace(dto.DeviceId))
            throw new InvalidDataException($"rule '{dto.Id}' contains an invalid deviceId.");

        var ruleKey = dto.Id.Trim();
        var name = dto.Name.Trim();
        var metric = Metric.Create(dto.Metric);
        var deviceId = string.IsNullOrWhiteSpace(dto.DeviceId) ? null : dto.DeviceId.Trim().ToUpperInvariant();
        var @operator = RuleOperator.Create(dto.Operator);
        var parameters = CreateParameters(dto);
        var hash = RuleConfigurationHasher.Compute(ruleKey, name, dto.Enabled.Value, metric.Value, deviceId, @operator.Value, parameters);

        return new RuleDefinition(ruleKey, name, dto.Enabled.Value, metric, deviceId, @operator, parameters, hash);
    }

    private static IReadOnlyCollection<RuleParameter> CreateParameters(RuleJsonDto dto)
    {
        if (ComparisonOperators.Contains(dto.Operator!))
            return [RuleParameter.Create(RuleParameterNames.Threshold, RequireFinite(dto.Threshold, dto.Id!, RuleParameterNames.Threshold))];

        if (dto.Operator == RuleOperatorNames.Between)
        {
            var lowerBound = RequireFinite(dto.LowerBound, dto.Id!, RuleParameterNames.LowerBound);
            var upperBound = RequireFinite(dto.UpperBound, dto.Id!, RuleParameterNames.UpperBound);

            if (lowerBound >= upperBound)
                throw new InvalidDataException($"Rule '{dto.Id}' requires lowerBound < upperBound.");

            return
            [
                RuleParameter.Create(RuleParameterNames.LowerBound, lowerBound),
                RuleParameter.Create(RuleParameterNames.UpperBound, upperBound)
            ];
        }

        if (dto.Operator == RuleOperatorNames.SustainedAbove)
        {
            var threshold = RequireFinite(dto.Threshold, dto.Id!, RuleParameterNames.Threshold);
            var durationSeconds = RequireFinite(dto.DurationSeconds, dto.Id!, RuleParameterNames.DurationSeconds);

            if (durationSeconds <= 0)
                throw new InvalidDataException($"Rule '{dto.Id}' requires a positive durationSeconds.");

            return
            [
                RuleParameter.Create(RuleParameterNames.Threshold, threshold),
                RuleParameter.Create(RuleParameterNames.DurationSeconds, durationSeconds)
            ];
        }

        throw new InvalidDataException($"Rule '{dto.Id}' uses unknown operator '{dto.Operator}'.");
    }

    private static double RequireFinite(double? value, string ruleId, string parameterName)
    {
        if (value is null || !double.IsFinite(value.Value))
            throw new InvalidDataException($"Rule '{ruleId}' requires a finite {parameterName}.");

        return value.Value;
    }
}
