using Microsoft.Extensions.Logging;

namespace SensorIngestion.Application.Ingestion;

public static class IngestionLogEvents
{
    public static readonly EventId RejectedRecord = new(1001, nameof(RejectedRecord));
    public static readonly EventId DuplicateConflict = new(1002, nameof(DuplicateConflict));
    public static readonly EventId RuleViolationEpisode = new(1003, nameof(RuleViolationEpisode));
    public static readonly EventId AlertEmitted = new(1004, nameof(AlertEmitted));
    public static readonly EventId AlertSuppressed = new(1005, nameof(AlertSuppressed));
    public static readonly EventId IngestionCompleted = new(1006, nameof(IngestionCompleted));
}
