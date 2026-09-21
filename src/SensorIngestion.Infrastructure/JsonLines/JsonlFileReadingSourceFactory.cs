using SensorIngestion.Application.Abstractions.Ingestion;

namespace SensorIngestion.Infrastructure.JsonLines;

public sealed class JsonlFileReadingSourceFactory : IReadingSourceFactory
{
    public IReadingSource Create(string path) => new JsonlFileReadingSource(path);
}
