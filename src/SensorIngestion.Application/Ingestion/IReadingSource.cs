namespace SensorIngestion.Application.Ingestion;

public interface IReadingSource
{
    IAsyncEnumerable<InputLine> ReadAsync(CancellationToken cancellationToken);
}
