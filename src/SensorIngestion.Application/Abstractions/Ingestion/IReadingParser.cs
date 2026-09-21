using SensorIngestion.Application.Ingestion;

namespace SensorIngestion.Application.Abstractions.Ingestion;

public interface IReadingParser
{
    ReadingParseResult Parse(InputLine inputLine);
}