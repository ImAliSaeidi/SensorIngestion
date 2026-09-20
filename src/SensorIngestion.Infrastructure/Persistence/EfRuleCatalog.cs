using Microsoft.EntityFrameworkCore;
using SensorIngestion.Application.Persistence;
using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Infrastructure.Persistence;

public sealed class EfRuleCatalog(SensorIngestionDbContext dbContext) : IRuleCatalog
{
    public async Task<IReadOnlyList<Rule>> SynchronizeAsync(IReadOnlyCollection<RuleDefinition> definitions, DateTimeOffset createdAt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var storedRules = await dbContext.Rules.OrderBy(x => x.RuleKey).ThenBy(x => x.Version).ToListAsync(cancellationToken);
        var result = new List<Rule>(definitions.Count);

        foreach (var definition in definitions)
        {
            var existing = storedRules.FirstOrDefault(x => x.RuleKey == definition.RuleKey && x.ConfigurationHash == definition.ConfigurationHash);
            if (existing is not null)
            {
                result.Add(existing);
                continue;
            }

            var version = storedRules.Where(x => x.RuleKey == definition.RuleKey).Select(x => x.Version).DefaultIfEmpty(0).Max() + 1;
            var rule = new Rule(
                definition.RuleKey,
                version,
                definition.Name,
                definition.Enabled,
                definition.Metric,
                definition.DeviceId,
                definition.Operator,
                definition.Parameters,
                definition.ConfigurationHash,
                createdAt);

            dbContext.Rules.Add(rule);
            storedRules.Add(rule);
            result.Add(rule);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return result.AsReadOnly();
    }
}
