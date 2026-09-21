using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.UnitTests.Domain.Readings;

public sealed class SensorReadingTests
{
    [Fact]
    public void Constructor_WhenArgumentsAreValid_ShouldCreatePendingReading()
    {
        var timestamp = new DateTimeOffset(2025, 6, 1, 12, 3, 4, TimeSpan.FromHours(3.5));

        var reading = new SensorReading("  pump-01  ", Metric.Temperature, timestamp, 67.21, 1199);

        Assert.Equal("PUMP-01", reading.DeviceId);
        Assert.Equal(Metric.Temperature, reading.Metric);
        Assert.Equal(TimeSpan.Zero, reading.Timestamp.Offset);
        Assert.Equal(timestamp.UtcDateTime, reading.Timestamp.UtcDateTime);
        Assert.Equal(67.21, reading.Value);
        Assert.Equal(1199, reading.Sequence);
        Assert.Equal(ReadingClassification.Pending, reading.Classification);
    }

    [Fact]
    public void Identity_WhenReadingIsValid_ShouldContainNaturalKey()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var reading = new SensorReading("PUMP-01", Metric.Pressure, timestamp, 5, 10);

        var identity = reading.Identity;

        Assert.Equal(reading.DeviceId, identity.DeviceId);
        Assert.Equal(reading.Metric, identity.Metric);
        Assert.Equal(reading.Timestamp, identity.Timestamp);
        Assert.Equal(reading.Sequence, identity.Sequence);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenDeviceIdIsBlank_ShouldThrowArgumentException(string? deviceId)
    {
        Assert.Throws<ArgumentException>(() => new SensorReading(deviceId!, Metric.Temperature, DateTimeOffset.UtcNow, 1, 1));
    }

    [Fact]
    public void Constructor_WhenDeviceIdIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SensorReading(null!, Metric.Temperature, DateTimeOffset.UtcNow, 1, 1));
    }

    [Fact]
    public void Constructor_WhenMetricIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SensorReading("PUMP-01", null!, DateTimeOffset.UtcNow, 1, 1));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WhenValueIsNotFinite_ShouldThrowArgumentException(double value)
    {
        Assert.Throws<ArgumentException>(() => new SensorReading("PUMP-01", Metric.Temperature, DateTimeOffset.UtcNow, value, 1));
    }

    [Fact]
    public void Constructor_WhenSequenceIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SensorReading("PUMP-01", Metric.Temperature, DateTimeOffset.UtcNow, 1, -1));
    }

    [Theory]
    [InlineData(false, ReadingClassification.Acceptable)]
    [InlineData(true, ReadingClassification.Unacceptable)]
    public void Classify_WhenCalled_ShouldSetExpectedClassification(bool hasViolation, ReadingClassification expected)
    {
        var reading = new SensorReading("PUMP-01", Metric.Temperature, DateTimeOffset.UtcNow, 1, 1);

        reading.Classify(hasViolation);

        Assert.Equal(expected, reading.Classification);
    }
}
