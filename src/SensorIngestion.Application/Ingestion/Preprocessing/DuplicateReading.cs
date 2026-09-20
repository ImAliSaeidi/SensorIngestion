using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Application.Ingestion.Preprocessing;

public sealed class DuplicateReading
{
    public SensorReading KeptReading { get; }

    public SensorReading DiscardedReading { get; }

    public bool HasConflictingValue => KeptReading.Value != DiscardedReading.Value;

    public DuplicateReading(SensorReading keptReading, SensorReading discardedReading)
    {
        ArgumentNullException.ThrowIfNull(keptReading);
        ArgumentNullException.ThrowIfNull(discardedReading);

        if (keptReading.Identity != discardedReading.Identity)
            throw new ArgumentException("readings must have same identity");

        KeptReading = keptReading;
        DiscardedReading = discardedReading;
    }
}
