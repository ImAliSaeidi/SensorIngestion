namespace SensorIngestion.Application.Aggregation;

public sealed record AggregationBucket(DateTimeOffset Start, int Count, double Average, double Minimum, double Maximum);
