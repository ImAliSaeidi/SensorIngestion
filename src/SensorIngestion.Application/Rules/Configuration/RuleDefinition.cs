using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Configuration;

public sealed class RuleDefinition
{
    public string RuleKey { get; }

    public string Name { get; }

    public bool Enabled { get; }

    public Metric Metric { get; }

    public string? DeviceId { get; }

    public RuleOperator Operator { get; }

    public IReadOnlyCollection<RuleParameter> Parameters { get; }

    public string ConfigurationHash { get; }

    public RuleDefinition(string ruleKey, string name, bool enabled, Metric metric, string? deviceId, RuleOperator @operator, IReadOnlyCollection<RuleParameter> parameters, string configurationHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(metric);
        ArgumentNullException.ThrowIfNull(@operator);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationHash);

        RuleKey = ruleKey.Trim();
        Name = name.Trim();
        Enabled = enabled;
        Metric = metric;
        DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
        Operator = @operator;
        Parameters = parameters.ToList().AsReadOnly();
        ConfigurationHash = configurationHash;
    }
}