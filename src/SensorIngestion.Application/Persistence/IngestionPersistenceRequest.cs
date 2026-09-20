using SensorIngestion.Application.Ingestion;
using SensorIngestion.Domain.Alerts;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Application.Persistence;

public sealed class IngestionPersistenceRequest
{
    public string FileFingerprint { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset CompletedAt { get; }

    public IReadOnlyCollection<SensorReading> Readings { get; }

    public IReadOnlyCollection<RuleEvaluationDraft> Evaluations { get; }

    public IReadOnlyCollection<Alert> Alerts { get; }

    public ProcessingReport Report { get; }

    public IngestionPersistenceRequest(string fileFingerprint, DateTimeOffset startedAt, DateTimeOffset completedAt, IReadOnlyCollection<SensorReading> readings, IReadOnlyCollection<RuleEvaluationDraft> evaluations, IReadOnlyCollection<Alert> alerts, ProcessingReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileFingerprint);
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentNullException.ThrowIfNull(evaluations);
        ArgumentNullException.ThrowIfNull(alerts);
        ArgumentNullException.ThrowIfNull(report);

        if (completedAt < startedAt)
            throw new ArgumentException("completion timestamp cannot be before start timestamp", nameof(completedAt));

        FileFingerprint = fileFingerprint.Trim();
        StartedAt = startedAt.ToUniversalTime();
        CompletedAt = completedAt.ToUniversalTime();
        Readings = readings;
        Evaluations = evaluations;
        Alerts = alerts;
        Report = report;
    }
}
