namespace SensorIngestion.Application.Abstractions.Ingestion;

public interface IReadingSourceFactory
{
    IReadingSource Create(string path);
}
