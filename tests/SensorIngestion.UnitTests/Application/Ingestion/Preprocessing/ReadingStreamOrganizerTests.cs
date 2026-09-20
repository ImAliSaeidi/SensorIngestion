using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.UnitTests.Application.Ingestion.Preprocessing;

public sealed class ReadingStreamOrganizerTests
{
    private static readonly DateTimeOffset Timestamp = new(2025, 6, 1, 8, 33, 0, TimeSpan.Zero);
    private readonly ReadingStreamOrganizer _organizer = new();

    [Fact]
    public void Organize_WhenInputIsEmpty_ShouldReturnNoStreams()
    {
        var streams = ReadingStreamOrganizer.Organize([]);

        Assert.Empty(streams);
    }

    [Fact]
    public void Organize_WhenDevicesOrMetricsDiffer_ShouldCreateIndependentStreams()
    {
        var pumpTemperature = CreateReading("PUMP-01", Metric.Temperature);
        var pumpPressure = CreateReading("PUMP-01", Metric.Pressure);
        var fanTemperature = CreateReading("FAN-01", Metric.Temperature);

        var streams = ReadingStreamOrganizer.Organize([pumpTemperature, pumpPressure, fanTemperature]);

        Assert.Equal(3, streams.Count);
        Assert.Same(pumpTemperature, Assert.Single(streams.Single(x => x.DeviceId == "PUMP-01" && x.Metric == Metric.Temperature).Readings));
        Assert.Same(pumpPressure, Assert.Single(streams.Single(x => x.DeviceId == "PUMP-01" && x.Metric == Metric.Pressure).Readings));
        Assert.Same(fanTemperature, Assert.Single(streams.Single(x => x.DeviceId == "FAN-01" && x.Metric == Metric.Temperature).Readings));
    }

    [Fact]
    public void Organize_WhenInputIsOutOfOrder_ShouldSortEachStreamByTimestampThenSequence()
    {
        var latest = CreateReading(timestamp: Timestamp.AddSeconds(20), sequence: 1);
        var sameTimestampLaterSequence = CreateReading(timestamp: Timestamp.AddSeconds(10), sequence: 2);
        var earliest = CreateReading(timestamp: Timestamp, sequence: 5);
        var sameTimestampEarlierSequence = CreateReading(timestamp: Timestamp.AddSeconds(10), sequence: 1);

        var stream = Assert.Single(ReadingStreamOrganizer.Organize([latest, sameTimestampLaterSequence, earliest, sameTimestampEarlierSequence]));

        Assert.Equal([earliest, sameTimestampEarlierSequence, sameTimestampLaterSequence, latest], stream.Readings);
    }

    [Fact]
    public void Organize_WhenStreamsAreInterleaved_ShouldOrderEachStreamIndependently()
    {
        var pumpLater = CreateReading("PUMP-01", Metric.Temperature, Timestamp.AddSeconds(10));
        var fanLater = CreateReading("FAN-01", Metric.Temperature, Timestamp.AddSeconds(20));
        var pumpEarlier = CreateReading("PUMP-01", Metric.Temperature, Timestamp);
        var fanEarlier = CreateReading("FAN-01", Metric.Temperature, Timestamp.AddSeconds(5));

        var streams = ReadingStreamOrganizer.Organize([pumpLater, fanLater, pumpEarlier, fanEarlier]);

        Assert.Equal([pumpEarlier, pumpLater], streams.Single(x => x.DeviceId == "PUMP-01").Readings);
        Assert.Equal([fanEarlier, fanLater], streams.Single(x => x.DeviceId == "FAN-01").Readings);
    }

    private static SensorReading CreateReading(string deviceId = "PUMP-01", Metric? metric = null, DateTimeOffset? timestamp = null, long sequence = 1)
    {
        return new SensorReading(deviceId, metric ?? Metric.Temperature, timestamp ?? Timestamp, 67.21, sequence);
    }
}
