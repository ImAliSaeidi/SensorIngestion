using SensorIngestion.Application.Abstractions.Ingestion;
using SensorIngestion.Application.Ingestion;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Infrastructure.JsonLines.Models;
using System.Text.Json;

namespace SensorIngestion.Infrastructure.JsonLines;

public class JsonlReadingParser : IReadingParser
{
    public ReadingParseResult Parse(InputLine inputLine)
    {
        if (string.IsNullOrWhiteSpace(inputLine.Content))
            return ReadingParseResult.Failure(new ReadingRejection(inputLine.LineNumber, RejectionCategory.EmptyLine, "line is empty"));

        try
        {
            var readingDto = JsonSerializer.Deserialize<ReadingJsonDto>(inputLine.Content);

            if (readingDto is null)
                return ReadingParseResult.Failure(new ReadingRejection(inputLine.LineNumber, RejectionCategory.InvalidRoot, "JSON root must be an object."));

            var validationError = readingDto.Validate();
            if (!string.IsNullOrWhiteSpace(validationError))
                return ReadingParseResult.Failure(new ReadingRejection(inputLine.LineNumber, RejectionCategory.InvalidFieldValue, validationError));

            var reading = new SensorReading(readingDto.DeviceId!, Metric.Create(readingDto.Metric!), readingDto.ParsedTimestamp, readingDto.Value!.Value, readingDto.Sequence!.Value);
            return ReadingParseResult.Success(reading);
        }
        catch (JsonException)
        {
            return ReadingParseResult.Failure(new ReadingRejection(inputLine.LineNumber, RejectionCategory.MalformedInput, "Line contains invalid JSON"));
        }
    }
}
