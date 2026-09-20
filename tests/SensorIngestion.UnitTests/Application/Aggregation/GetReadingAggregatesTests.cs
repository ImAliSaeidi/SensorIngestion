using SensorIngestion.Application.Aggregation;
using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.UnitTests.Application.Aggregation;

public sealed class GetReadingAggregatesTests
{
    private static readonly DateTimeOffset From = new(2025, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_WhenReadingsSpanBuckets_ShouldCalculateBucketStatistics()
    {
        var store = new StubStore(
        [
            new AcceptableReadingValue(From, 10),
            new AcceptableReadingValue(From.AddSeconds(59), 20),
            new AcceptableReadingValue(From.AddSeconds(60), 30),
            new AcceptableReadingValue(From.AddSeconds(119), 50)
        ]);

        var result = await new GetReadingAggregates(store).ExecuteAsync(Query(), CancellationToken.None);

        Assert.Collection(result,
            bucket => AssertBucket(bucket, From, 2, 15, 10, 20),
            bucket => AssertBucket(bucket, From.AddSeconds(60), 2, 40, 30, 50));
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoAcceptableReadingsExist_ShouldOmitEmptyBuckets()
    {
        var result = await new GetReadingAggregates(new StubStore([])).ExecuteAsync(Query(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassHalfOpenRangeAndStreamIdentityToStore()
    {
        var store = new StubStore([]);
        var query = Query();

        await new GetReadingAggregates(store).ExecuteAsync(query, CancellationToken.None);

        Assert.Equal(query.DeviceId, store.DeviceId);
        Assert.Equal(query.Metric, store.Metric);
        Assert.Equal(query.From, store.From);
        Assert.Equal(query.To, store.To);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFromIsNotEarlierThanTo_ShouldThrowArgumentException()
    {
        var query = Query(from: From.AddMinutes(2), to: From.AddMinutes(2));

        await Assert.ThrowsAsync<ArgumentException>(() => new GetReadingAggregates(new StubStore([])).ExecuteAsync(query, CancellationToken.None));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteAsync_WhenBucketSecondsIsNotPositive_ShouldThrowArgumentOutOfRangeException(int bucketSeconds)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => new GetReadingAggregates(new StubStore([])).ExecuteAsync(Query(bucketSeconds: bucketSeconds), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_WhenTimestampIsNotUtc_ShouldThrowArgumentException()
    {
        var query = Query(from: From.ToOffset(TimeSpan.FromHours(3.5)));

        await Assert.ThrowsAsync<ArgumentException>(() => new GetReadingAggregates(new StubStore([])).ExecuteAsync(query, CancellationToken.None));
    }

    private static AggregationQuery Query(DateTimeOffset? from = null, DateTimeOffset? to = null, int bucketSeconds = 60)
        => new("PUMP-01", Metric.Temperature, from ?? From, to ?? From.AddMinutes(2), bucketSeconds);

    private static void AssertBucket(AggregationBucket bucket, DateTimeOffset start, int count, double average, double minimum, double maximum)
    {
        Assert.Equal(start, bucket.Start);
        Assert.Equal(count, bucket.Count);
        Assert.Equal(average, bucket.Average);
        Assert.Equal(minimum, bucket.Minimum);
        Assert.Equal(maximum, bucket.Maximum);
    }

    private sealed class StubStore(IReadOnlyList<AcceptableReadingValue> readings) : IReadingAggregationStore
    {
        public string? DeviceId { get; private set; }
        public Metric? Metric { get; private set; }
        public DateTimeOffset? From { get; private set; }
        public DateTimeOffset? To { get; private set; }

        public Task<IReadOnlyList<AcceptableReadingValue>> QueryAcceptableAsync(string deviceId, Metric metric, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        {
            DeviceId = deviceId;
            Metric = metric;
            From = from;
            To = to;
            return Task.FromResult(readings);
        }
    }
}
