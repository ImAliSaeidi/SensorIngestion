using SensorIngestion.Application.Rules.Configuration;

namespace SensorIngestion.Application.Abstractions.Rules.Configuration;

public interface IRuleConfigurationLoader
{
    Task<IReadOnlyList<RuleDefinition>> LoadAsync(CancellationToken cancellationToken);
}
