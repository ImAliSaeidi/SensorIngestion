using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Persistence;

public interface IRuleCatalog
{
    Task<IReadOnlyList<Rule>> SynchronizeAsync(IReadOnlyCollection<RuleDefinition> definitions, DateTimeOffset createdAt, CancellationToken cancellationToken);
}
