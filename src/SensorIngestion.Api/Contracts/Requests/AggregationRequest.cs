namespace SensorIngestion.Api.Contracts.Requests;

public sealed class AggregationRequest
{
    public string? DeviceId { get; init; }

    public string? Metric { get; init; }

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public int? BucketSeconds { get; init; }
}
