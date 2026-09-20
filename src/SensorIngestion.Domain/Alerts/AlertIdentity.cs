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
        if (ruleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(ruleId), "alert identity rule id is required");

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentNullException(nameof(deviceId), "alert identity device id is required");

        RuleId = ruleId;
        DeviceId = deviceId.Trim().ToUpperInvariant();
        Metric = metric ?? throw new ArgumentNullException(nameof(metric), "alert identity metric is required");
        StartTimestamp = startTimestamp.ToUniversalTime();
    }
}
