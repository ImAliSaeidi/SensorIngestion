using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.UnitTests.Application.Ingestion.Preprocessing;

public sealed class ReadingDeduplicatorTests
{
    private static readonly DateTimeOffset Timestamp = new(2025, 6, 1, 8, 33, 0, TimeSpan.Zero);
    private readonly ReadingDeduplicator _deduplicator = new();

    [Fact]
    public void Deduplicate_WhenInputIsEmpty_ShouldReturnEmptyResult()
    {
        var result = ReadingDeduplicator.Deduplicate([]);

        Assert.Empty(result.UniqueReadings);
        Assert.Empty(result.Duplicates);
    }

    [Fact]
    public void Deduplicate_WhenReadingsAreUnique_ShouldPreserveAllReadingsInInputOrder()
    {
        var first = CreateReading(sequence: 1);
        var second = CreateReading(sequence: 2);

        var result = ReadingDeduplicator.Deduplicate([first, second]);

        Assert.Equal([first, second], result.UniqueReadings);
        Assert.Empty(result.Duplicates);
    }

    [Fact]
    public void Deduplicate_WhenIdenticalIdentityAppearsAgain_ShouldKeepFirstReading()
    {
        var first = CreateReading(value: 67.21);
        var duplicate = CreateReading(value: 67.21);

        var result = ReadingDeduplicator.Deduplicate([first, duplicate]);

        Assert.Same(first, Assert.Single(result.UniqueReadings));
        var duplicateResult = Assert.Single(result.Duplicates);
        Assert.Same(first, duplicateResult.KeptReading);
        Assert.Same(duplicate, duplicateResult.DiscardedReading);
        Assert.False(duplicateResult.HasConflictingValue);
    }

    [Fact]
    public void Deduplicate_WhenDuplicateHasDifferentValue_ShouldKeepFirstAndMarkConflict()
    {
        var first = CreateReading(value: 67.21);
        var duplicate = CreateReading(value: 91.40);

        var result = ReadingDeduplicator.Deduplicate([first, duplicate]);

        Assert.Same(first, Assert.Single(result.UniqueReadings));
        var duplicateResult = Assert.Single(result.Duplicates);
        Assert.Same(first, duplicateResult.KeptReading);
        Assert.Same(duplicate, duplicateResult.DiscardedReading);
        Assert.True(duplicateResult.HasConflictingValue);
    }

    [Fact]
    public void Deduplicate_WhenSeveralCopiesExist_ShouldReportEveryDiscardedReading()
    {
        var first = CreateReading(value: 67.21);
        var second = CreateReading(value: 67.21);
        var third = CreateReading(value: 80);

        var result = ReadingDeduplicator.Deduplicate([first, second, third]);

        Assert.Same(first, Assert.Single(result.UniqueReadings));
        Assert.Collection(result.Duplicates,
            duplicate => Assert.False(duplicate.HasConflictingValue),
            duplicate => Assert.True(duplicate.HasConflictingValue));
    }

    [Fact]
    public void Deduplicate_WhenAnyIdentityComponentDiffers_ShouldKeepEveryReading()
    {
        var readings = new[]
        {
            CreateReading(),
            CreateReading(deviceId: "PUMP-02"),
            CreateReading(metric: Metric.Pressure),
            CreateReading(timestamp: Timestamp.AddSeconds(1)),
            CreateReading(sequence: 2)
        };

        var result = ReadingDeduplicator.Deduplicate(readings);

        Assert.Equal(readings, result.UniqueReadings);
        Assert.Empty(result.Duplicates);
    }

    private static SensorReading CreateReading(string deviceId = "PUMP-01", Metric? metric = null, DateTimeOffset? timestamp = null, double value = 67.21, long sequence = 1)
    {
        return new SensorReading(deviceId, metric ?? Metric.Temperature, timestamp ?? Timestamp, value, sequence);
    }
}
