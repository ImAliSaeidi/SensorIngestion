using SensorIngestion.Domain.Common;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Domain.Rules;

public sealed class Rule : Entity
{
    private readonly List<RuleParameter> _parameters = [];

    public string RuleKey { get; private set; } = null!;

    public int Version { get; private set; }

    public string Name { get; private set; } = null!;

    public bool Enabled { get; private set; }

    public Metric Metric { get; private set; } = null!;

    public string? DeviceId { get; private set; }

    public RuleOperator Operator { get; private set; } = null!;

    public IReadOnlyCollection<RuleParameter> Parameters => _parameters.AsReadOnly();

    public string ConfigurationHash { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    private Rule() { }

    public Rule(string ruleKey, int version, string name, bool enabled, Metric metric, string? deviceId, RuleOperator @operator, IEnumerable<RuleParameter> parameters, string configurationHash, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleKey);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(version, 0);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationHash);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(metric);
        ArgumentNullException.ThrowIfNull(@operator);

        var parametersList = parameters.ToList();

        var hasDuplicateParameters = parametersList
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > 1);

        if (hasDuplicateParameters)
            throw new ArgumentException("rule parameters must have unique names", nameof(parameters));

        RuleKey = ruleKey.Trim();
        Version = version;
        Name = name.Trim();
        Enabled = enabled;
        Metric = metric;
        DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim().ToUpperInvariant();
        Operator = @operator;
        ConfigurationHash = configurationHash.Trim();
        CreatedAt = createdAt.ToUniversalTime();
        _parameters.AddRange(parametersList);
    }

    public bool AppliesTo(SensorReading reading)
    {
        return
            Enabled &&
            Metric == reading.Metric &&
            (DeviceId == null || DeviceId.Equals(reading.DeviceId, StringComparison.OrdinalIgnoreCase));
    }
}
