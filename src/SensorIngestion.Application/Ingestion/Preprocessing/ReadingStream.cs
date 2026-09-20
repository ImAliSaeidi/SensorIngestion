using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Application.Ingestion.Preprocessing;

public sealed class ReadingStream
{
    public string DeviceId { get; }

    public Metric Metric { get; }

    public IReadOnlyList<SensorReading> Readings { get; }

    internal ReadingStream(string deviceId, Metric metric, IEnumerable<SensorReading> readings)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
            throw new ArgumentNullException(nameof(deviceId), "device id is required");

        ArgumentNullException.ThrowIfNull(metric);
        ArgumentNullException.ThrowIfNull(readings);

        DeviceId = deviceId;
        Metric = metric;
        Readings = readings.ToList().AsReadOnly();
    }
}
