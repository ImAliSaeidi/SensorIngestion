using SensorIngestion.Application.Abstractions.Aggregation;

namespace SensorIngestion.Application.Aggregation;

public sealed class GetReadingAggregates(IReadingAggregationStore store)
{
    public async Task<IReadOnlyList<AggregationBucket>> ExecuteAsync(AggregationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.DeviceId);
        ArgumentNullException.ThrowIfNull(query.Metric);

        if (query.From.Offset != TimeSpan.Zero)
            throw new ArgumentException("from timestamp must be UTC", nameof(query));

        if (query.To.Offset != TimeSpan.Zero)
            throw new ArgumentException("to timestamp must be UTC", nameof(query));

        if (query.From >= query.To)
            throw new ArgumentException("from timestamp must be earlier than to timestamp", nameof(query));

        if (query.BucketSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(query), "bucket seconds must be greater than zero");

        var bucketDuration = TimeSpan.FromSeconds(query.BucketSeconds);
        var readings = await store.QueryAcceptableAsync(query.DeviceId.Trim(), query.Metric, query.From, query.To, cancellationToken);

        // The store already applies the half-open [from, to) range. Bucket indexes
        // are calculated from the requested start, so bucket boundaries are stable.
        return readings
            .GroupBy(reading => (long)((reading.Timestamp - query.From).Ticks / bucketDuration.Ticks))
            .OrderBy(group => group.Key)
            .Select(group => new AggregationBucket(
                query.From.AddTicks(group.Key * bucketDuration.Ticks),
                group.Count(),
                group.Average(x => x.Value),
                group.Min(x => x.Value),
                group.Max(x => x.Value)))
            .ToList()
            .AsReadOnly();
    }
}
