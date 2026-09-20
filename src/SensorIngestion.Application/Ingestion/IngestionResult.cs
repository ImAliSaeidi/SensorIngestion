using SensorIngestion.Domain.Alerts;

namespace SensorIngestion.Application.Ingestion;

public sealed class IngestionResult(ProcessingReport report, IReadOnlyCollection<ReadingRejection> rejections, IReadOnlyCollection<Alert> alerts)
{
    public ProcessingReport Report { get; } = report;

    public IReadOnlyList<ReadingRejection> Rejections { get; } = rejections.ToList().AsReadOnly();

    public IReadOnlyList<Alert> Alerts { get; } = alerts.ToList().AsReadOnly();
}
