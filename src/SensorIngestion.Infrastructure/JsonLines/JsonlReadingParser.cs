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

        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(inputLine.Content);
        }
        catch (JsonException)
        {
            return ReadingParseResult.Failure(new ReadingRejection(inputLine.LineNumber, RejectionCategory.MalformedInput, "Line contains invalid JSON"));
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return ReadingParseResult.Failure(new ReadingRejection(inputLine.LineNumber, RejectionCategory.InvalidRoot, "JSON root must be an object."));

            ReadingJsonDto? readingDTO;

            try
            {
                readingDTO = JsonSerializer.Deserialize<ReadingJsonDto>(inputLine.Content);
            }
            catch (JsonException)
            {
                return ReadingParseResult.Failure(new ReadingRejection(inputLine.LineNumber, RejectionCategory.InvalidFieldType, "reading contains a field with an invalid type"));
            }

            if (readingDTO == null)
                return ReadingParseResult.Failure(new ReadingRejection(inputLine.LineNumber, RejectionCategory.EmptyLine, "line is null"));

            var validationError = readingDTO.Validate();
            if (!string.IsNullOrWhiteSpace(validationError))
                return ReadingParseResult.Failure(new ReadingRejection(inputLine.LineNumber, RejectionCategory.InvalidFieldValue, validationError));

            var reading = new SensorReading(readingDTO.DeviceId!, Metric.Create(readingDTO.Metric!), readingDTO.ParsedTimestamp, readingDTO.Value!.Value, readingDTO.Sequence!.Value);
            return ReadingParseResult.Success(reading);
        }
    }
}
