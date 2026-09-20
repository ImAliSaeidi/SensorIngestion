namespace SensorIngestion.Application.Ingestion;

public interface IReadingParser
{
    ReadingParseResult Parse(InputLine inputLine);
}