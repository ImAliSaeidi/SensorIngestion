using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Application.Ingestion;

public sealed class ReadingParseResult
{
    public SensorReading? Reading { get; private set; }

    public ReadingRejection? Rejection { get; private set; }

    public bool WasParsed => IsSuccess || Rejection?.Category is not (RejectionCategory.EmptyLine or RejectionCategory.MalformedInput or RejectionCategory.InvalidRoot);

    public bool IsSuccess => Reading != null;

    private ReadingParseResult(SensorReading? reading, ReadingRejection? rejection)
    {
        Reading = reading;
        Rejection = rejection;
    }

    public static ReadingParseResult Success(SensorReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return new ReadingParseResult(reading, null);
    }

    public static ReadingParseResult Failure(ReadingRejection rejection)
    {
        ArgumentNullException.ThrowIfNull(rejection);
        return new ReadingParseResult(null, rejection);
    }
}
