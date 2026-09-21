using SensorIngestion.Application.Ingestion;

namespace SensorIngestion.Application.Abstractions.Ingestion;

public interface IReadingSource
{
    IAsyncEnumerable<InputLine> ReadAsync(CancellationToken cancellationToken);

    ValueTask<string> GetFingerprintAsync(CancellationToken cancellationToken);
}
