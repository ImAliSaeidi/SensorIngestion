namespace SensorIngestion.Application.Rules.Configuration;

public interface IRuleConfigurationLoader
{
    Task<IReadOnlyList<RuleDefinition>> LoadAsync(CancellationToken cancellationToken);
}
