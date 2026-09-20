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
        if (string.IsNullOrWhiteSpace(ruleKey))
            throw new ArgumentNullException(nameof(ruleKey), "rule key is required");

        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version), "rule version must be grater than zero");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name), "rule name is required");

        if (string.IsNullOrWhiteSpace(configurationHash))
            throw new ArgumentNullException(nameof(configurationHash), "rule configuration hash is required");

        if (parameters == null)
            throw new ArgumentNullException(nameof(parameters), "rule parameters is required");

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
        Metric = metric ?? throw new ArgumentNullException(nameof(metric), "rule metric is required");
        DeviceId = string.IsNullOrWhiteSpace(deviceId) ? null : deviceId.Trim();
        Operator = @operator ?? throw new ArgumentNullException(nameof(@operator), "rule operator is required");
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
