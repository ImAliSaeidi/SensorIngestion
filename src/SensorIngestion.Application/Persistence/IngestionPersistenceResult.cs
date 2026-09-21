using SensorIngestion.Application.Ingestion;
using SensorIngestion.Domain.Alerts;

namespace SensorIngestion.Application.Persistence;

public sealed record IngestionPersistenceResult(
    int StoredReadings,
    int StoredEvaluations,
    IReadOnlyList<Alert> PersistedAlerts,
    ProcessingReport Report)
{
    public int StoredAlerts => PersistedAlerts.Count;
}
