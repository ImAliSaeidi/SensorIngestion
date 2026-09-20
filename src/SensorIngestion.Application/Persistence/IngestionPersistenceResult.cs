using SensorIngestion.Application.Ingestion;

namespace SensorIngestion.Application.Persistence;

public sealed record IngestionPersistenceResult(int StoredReadings, int StoredEvaluations, int StoredAlerts, ProcessingReport Report);
