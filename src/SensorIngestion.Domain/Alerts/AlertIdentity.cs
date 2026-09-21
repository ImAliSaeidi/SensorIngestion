using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.Domain.Alerts;

public sealed record AlertIdentity
{
    public long RuleId { get; }

    public string DeviceId { get; }

    public Metric Metric { get; }

    public DateTimeOffset StartTimestamp { get; }

    public AlertIdentity(long ruleId, string deviceId, Metric metric, DateTimeOffset startTimestamp)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ruleId, 0);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        ArgumentNullException.ThrowIfNull(metric);

        RuleId = ruleId;
        DeviceId = deviceId.Trim().ToUpperInvariant();
        Metric = metric;
        StartTimestamp = startTimestamp.ToUniversalTime();
    }
}
