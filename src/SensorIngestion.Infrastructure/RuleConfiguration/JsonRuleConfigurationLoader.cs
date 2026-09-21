using SensorIngestion.Application.Abstractions.Rules.Configuration;
using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Infrastructure.RuleConfiguration.Models;
using System.Text.Json;

namespace SensorIngestion.Infrastructure.RuleConfiguration;

public class JsonRuleConfigurationLoader : IRuleConfigurationLoader
{
    private readonly string _path;

    public JsonRuleConfigurationLoader(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

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

        return RuleDefinitionFactory.CreateAll(ruleDtos);
    }
}
