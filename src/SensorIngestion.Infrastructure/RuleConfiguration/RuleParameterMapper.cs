using SensorIngestion.Application.Rules;
using SensorIngestion.Domain.Rules;
using SensorIngestion.Infrastructure.RuleConfiguration.Models;

namespace SensorIngestion.Infrastructure.RuleConfiguration;

internal static class RuleParameterMapper
{
    public static IReadOnlyCollection<RuleParameter> Map(RuleJsonDto dto)
    {
        return dto.Operator switch
        {
            RuleOperatorNames.GreaterThan
                or RuleOperatorNames.GreaterThanOrEqual
                or RuleOperatorNames.LessThan
                or RuleOperatorNames.LessThanOrEqual
                or RuleOperatorNames.Equal =>
            [
                RuleParameter.Create(RuleParameterNames.Threshold, dto.Threshold!.Value)
            ],

            RuleOperatorNames.Between =>
            [
                RuleParameter.Create(RuleParameterNames.LowerBound, dto.LowerBound!.Value),
                RuleParameter.Create(RuleParameterNames.UpperBound, dto.UpperBound!.Value)
            ],

            RuleOperatorNames.SustainedAbove =>
            [
                RuleParameter.Create(RuleParameterNames.Threshold, dto.Threshold!.Value),
                RuleParameter.Create(RuleParameterNames.DurationSeconds, dto.DurationSeconds!.Value)
            ],

            _ => throw new InvalidDataException($"Unknown operator '{dto.Operator}'.")
        };
    }
}
