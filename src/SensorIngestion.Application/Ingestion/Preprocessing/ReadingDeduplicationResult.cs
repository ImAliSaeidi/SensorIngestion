using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Application.Ingestion.Preprocessing;

public sealed class ReadingDeduplicationResult
{
    public IReadOnlyCollection<SensorReading> UniqueReadings { get; }

    public IReadOnlyCollection<DuplicateReading> Duplicates { get; }

    public ReadingDeduplicationResult(IEnumerable<SensorReading> uniqueReadings, IEnumerable<DuplicateReading> duplicates)
    {
        ArgumentNullException.ThrowIfNull(uniqueReadings);
        ArgumentNullException.ThrowIfNull(duplicates);

        UniqueReadings = uniqueReadings.ToList().AsReadOnly();
        Duplicates = duplicates.ToList().AsReadOnly();
    }
}
