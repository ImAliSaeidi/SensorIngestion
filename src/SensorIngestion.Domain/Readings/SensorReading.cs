using SensorIngestion.Domain.Common;
using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.Domain.Readings;

public sealed class SensorReading : Entity
{
    public string DeviceId { get; private set; } = null!;

    public Metric Metric { get; private set; } = null!;

    public DateTimeOffset Timestamp { get; private set; }

    public double Value { get; private set; }

    public long Sequence { get; private set; }

    public ReadingClassification Classification { get; private set; }

    public ReadingIdentity Identity => new(DeviceId, Metric, Timestamp, Sequence);

    private SensorReading() { }


    public SensorReading(string deviceId, Metric metric, DateTimeOffset timestamp, double value, long sequence)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentNullException(nameof(deviceId), "device id cannot be null");

        if (!double.IsFinite(value))
            throw new ArgumentException("sensor value must be finite", nameof(value));

        DeviceId = deviceId.Trim();
        Metric = metric ?? throw new ArgumentNullException(nameof(metric), "metric cannot be null");
        Timestamp = timestamp.ToUniversalTime();
        Value = value;
        Sequence = sequence;
        Classification = ReadingClassification.Pending;
    }

    public void Classify(bool hasViolation)
    {
        Classification = hasViolation
             ? ReadingClassification.Unacceptable
             : ReadingClassification.Acceptable;
    }
}


public sealed record ReadingIdentity(string DeviceId, Metric Metric, DateTimeOffset Timestamp, long Sequence);
