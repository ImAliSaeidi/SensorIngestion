namespace SensorIngestion.Application.Ingestion;

public interface IReadingSourceFactory
{
    IReadingSource Create(string path);
}
