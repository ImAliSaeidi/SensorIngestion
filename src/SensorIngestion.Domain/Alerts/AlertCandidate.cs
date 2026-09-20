using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.Domain.Alerts;

public sealed record AlertCandidate
{
    public long RuleId { get; }

    public string DeviceId { get; }

    public Metric Metric { get; }

    public DateTimeOffset StartTimestamp { get; }

    public DateTimeOffset EndTimestamp { get; }

    public double? PeakValue { get; }

    public bool IsOpen { get; }

    public AlertIdentity Identity => new(RuleId, DeviceId, Metric, StartTimestamp);

    public AlertCandidate(long ruleId, string deviceId, Metric metric, DateTimeOffset startTimestamp, DateTimeOffset endTimestamp, double? peakValue, bool isOpen)
    {
        if (ruleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(ruleId), "alert candidate rule id is required");

        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentNullException(nameof(deviceId), "alert candidate device id is required");

        if (endTimestamp < startTimestamp)
            throw new ArgumentException("end timestamp cannot be before start timestamp");

        if (peakValue.HasValue && !double.IsFinite(peakValue.Value))
            throw new ArgumentException("peak value must be finite", nameof(peakValue));

        RuleId = ruleId;
        DeviceId = deviceId.Trim();
        Metric = metric ?? throw new ArgumentNullException(nameof(metric), "alert candidate metric is required");
        StartTimestamp = startTimestamp.ToUniversalTime();
        EndTimestamp = endTimestamp.ToUniversalTime();
        PeakValue = peakValue;
        IsOpen = isOpen;
    }
}
