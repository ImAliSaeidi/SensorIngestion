namespace SensorIngestion.Application.Persistence;

public interface IIngestionPersistence
{
    Task<IngestionPersistenceResult> PersistAsync(IngestionPersistenceRequest request, CancellationToken cancellationToken);
}
