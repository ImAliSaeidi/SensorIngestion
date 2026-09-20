using SensorIngestion.Application.Ingestion;

namespace SensorIngestion.Infrastructure.Persistence;

public sealed class IngestionRunRecord
{
    public long Id { get; private set; }

    public string FileFingerprint { get; private set; } = null!;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string Status { get; private set; } = null!;

    public string? FailureReason { get; private set; }

    public int TotalLinesRead { get; private set; }

    public int ParsedReadings { get; private set; }

    public int StoredReadings { get; private set; }

    public int DuplicatesRemoved { get; private set; }

    public int InvalidRecordsRejected { get; private set; }

    public int RulesLoaded { get; private set; }

    public int RuleEvaluationsPerformed { get; private set; }

    public int AcceptableReadings { get; private set; }

    public int UnacceptableReadings { get; private set; }

    public int RuleViolations { get; private set; }

    public int AlertsGenerated { get; private set; }

    private IngestionRunRecord() { }

    public static IngestionRunRecord Start(string fileFingerprint, DateTimeOffset startedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileFingerprint);
        return new IngestionRunRecord { FileFingerprint = fileFingerprint.Trim(), StartedAt = startedAt.ToUniversalTime(), Status = "Started" };
    }

    public void Complete(DateTimeOffset completedAt, ProcessingReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        CompletedAt = completedAt.ToUniversalTime();
        Status = "Completed";
        TotalLinesRead = report.TotalLinesRead;
        ParsedReadings = report.ParsedReadings;
        StoredReadings = report.StoredReadings;
        DuplicatesRemoved = report.DuplicatesRemoved;
        InvalidRecordsRejected = report.InvalidRecordsRejected;
        RulesLoaded = report.RulesLoaded;
        RuleEvaluationsPerformed = report.RuleEvaluationsPerformed;
        AcceptableReadings = report.AcceptableReadings;
        UnacceptableReadings = report.UnacceptableReadings;
        RuleViolations = report.RuleViolations;
        AlertsGenerated = report.AlertsGenerated;
    }

    public void Fail(DateTimeOffset completedAt, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        CompletedAt = completedAt.ToUniversalTime();
        Status = "Failed";
        FailureReason = reason.Trim();
    }
}
