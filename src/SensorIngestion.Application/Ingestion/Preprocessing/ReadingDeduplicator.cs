using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Application.Ingestion.Preprocessing;

public sealed class ReadingDeduplicator
{
    public static ReadingDeduplicationResult Deduplicate(IEnumerable<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var seen = new Dictionary<ReadingIdentity, SensorReading>();
        var uniqueReadings = new List<SensorReading>();
        var duplicates = new List<DuplicateReading>();

        foreach (var reading in readings)
        {
            // The first valid occurrence wins. A later duplicate is still tracked
            // so conflicting values can be reported without changing the result.
            if (seen.TryAdd(reading.Identity, reading))
            {
                uniqueReadings.Add(reading);
                continue;
            }

            var keptReading = seen[reading.Identity];
            duplicates.Add(new DuplicateReading(keptReading, reading));
        }

        return new ReadingDeduplicationResult(uniqueReadings, duplicates);
    }
}
