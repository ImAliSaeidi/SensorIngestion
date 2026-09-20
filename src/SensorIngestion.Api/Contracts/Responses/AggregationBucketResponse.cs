namespace SensorIngestion.Api.Contracts.Responses;

public sealed record AggregationBucketResponse(DateTimeOffset Start, int Count, double Average, double Minimum, double Maximum);
