using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Application.Ingestion.Preprocessing;

public sealed class ReadingStreamOrganizer
{
    public static IReadOnlyList<ReadingStream> Organize(IEnumerable<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        // Stateful rules work on one ordered event-time stream per device/metric.
        return readings
            .GroupBy(reading => new { reading.DeviceId, reading.Metric })
            .OrderBy(group => group.Key.DeviceId, StringComparer.Ordinal)
            .ThenBy(group => group.Key.Metric.Value, StringComparer.Ordinal)
            .Select(group => new ReadingStream(
                group.Key.DeviceId,
                group.Key.Metric,
                group.OrderBy(reading => reading.Timestamp)
                    .ThenBy(reading => reading.Sequence)))
            .ToList()
            .AsReadOnly();
    }
}
