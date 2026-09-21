using SensorIngestion.Application.Persistence;

namespace SensorIngestion.Application.Abstractions.Persistence;

public interface IIngestionPersistence
{
    Task<IngestionPersistenceResult> PersistAsync(IngestionPersistenceRequest request, CancellationToken cancellationToken);
}
