namespace SensorIngestion.Application.Aggregation;

public sealed record AcceptableReadingValue(DateTimeOffset Timestamp, double Value);
