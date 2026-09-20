using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Rules;
using SensorIngestion.Infrastructure.RuleConfiguration.Models;
using System.Text.Json;

namespace SensorIngestion.Infrastructure.RuleConfiguration;

public class JsonRuleConfigurationLoader : IRuleConfigurationLoader
{
    private readonly string _path;

    public JsonRuleConfigurationLoader(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Rule configuration path is required.", nameof(path));

        _path = path;
    }

    public async Task<IReadOnlyList<RuleDefinition>> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<RuleJsonDto>? ruleDtos;

        try
        {
            await using var stream = File.OpenRead(_path);
            ruleDtos = await JsonSerializer.DeserializeAsync<List<RuleJsonDto>>(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Rule configuration contains invalid JSON.", exception);
        }

        if (ruleDtos is null)
            throw new InvalidDataException("Rule configuration must contain a JSON array.");

        RuleConfigurationValidator.Validate(ruleDtos);

        return ruleDtos
            .Select(CreateDefinition)
            .ToList()
            .AsReadOnly();
    }

    private static RuleDefinition CreateDefinition(RuleJsonDto dto)
    {
        var ruleKey = dto.Id!.Trim();
        var name = dto.Name!.Trim();
        var metric = Metric.Create(dto.Metric!);
        var deviceId = string.IsNullOrWhiteSpace(dto.DeviceId) ? null : dto.DeviceId.Trim();
        var @operator = RuleOperator.Create(dto.Operator!);
        var parameters = CreateParameters(dto);

        var hash = RuleConfigurationHasher.Compute(
            ruleKey,
            name,
            dto.Enabled!.Value,
            metric.Value,
            deviceId,
            @operator.Value,
            parameters);

        return new RuleDefinition(
            ruleKey,
            name,
            dto.Enabled.Value,
            metric,
            deviceId,
            @operator,
            parameters,
            hash);
    }

    private static IReadOnlyCollection<RuleParameter> CreateParameters(RuleJsonDto dto)
    {
        if (dto.Operator is "GreaterThan" or "GreaterThanOrEqual" or "LessThan" or "LessThanOrEqual" or "Equal")
            return [RuleParameter.Create("threshold", dto.Threshold!.Value)];

        if (dto.Operator == "Between")
        {
            return
            [
                RuleParameter.Create("lowerBound", dto.LowerBound!.Value),
            RuleParameter.Create("upperBound", dto.UpperBound!.Value)
            ];
        }

        if (dto.Operator == "SustainedAbove")
        {
            return
            [
                RuleParameter.Create("threshold", dto.Threshold!.Value),
            RuleParameter.Create("durationSeconds", dto.DurationSeconds!.Value)
            ];
        }

        throw new InvalidDataException($"Unknown operator '{dto.Operator}'.");
    }
}
