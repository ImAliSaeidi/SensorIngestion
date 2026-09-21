using SensorIngestion.Domain.Common;
using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.Domain.Alerts;

public class Alert : Entity
{
    public long RuleId { get; private set; }

    public string DeviceId { get; private set; } = null!;

    public Metric Metric { get; private set; } = null!;

    public DateTimeOffset StartTimestamp { get; private set; }

    public DateTimeOffset EndTimestamp { get; private set; }

    public double? PeakValue { get; private set; }

    public bool IsOpen { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public AlertIdentity Identity => new(RuleId, DeviceId, Metric, StartTimestamp);

    private Alert() { }

    private Alert(AlertCandidate candidate, DateTimeOffset createdAt)
    {
        RuleId = candidate.RuleId;
        DeviceId = candidate.DeviceId;
        Metric = candidate.Metric;
        StartTimestamp = candidate.StartTimestamp;
        EndTimestamp = candidate.EndTimestamp;
        PeakValue = candidate.PeakValue;
        IsOpen = candidate.IsOpen;
        CreatedAt = createdAt.ToUniversalTime();
    }

    public static Alert Create(AlertCandidate candidate, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return new Alert(candidate, createdAt);
    }
}
