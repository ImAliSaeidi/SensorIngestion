using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.Application.Aggregation;

public sealed record AggregationQuery(string DeviceId, Metric Metric, DateTimeOffset From, DateTimeOffset To, int BucketSeconds);
