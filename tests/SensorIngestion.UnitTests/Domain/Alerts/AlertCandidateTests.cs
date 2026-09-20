using SensorIngestion.Domain.Alerts;
using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.UnitTests.Domain.Alerts;

public sealed class AlertCandidateTests
{
    [Fact]
    public void Constructor_WhenArgumentsAreValid_ShouldCreateNormalizedCandidate()
    {
        var start = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.FromHours(3.5));
        var end = start.AddMinutes(1);

        var candidate = new AlertCandidate(1, "  PUMP-01  ", Metric.Temperature, start, end, 90, true);

        Assert.Equal(1, candidate.RuleId);
        Assert.Equal("PUMP-01", candidate.DeviceId);
        Assert.Equal(Metric.Temperature, candidate.Metric);
        Assert.Equal(TimeSpan.Zero, candidate.StartTimestamp.Offset);
        Assert.Equal(TimeSpan.Zero, candidate.EndTimestamp.Offset);
        Assert.Equal(start.UtcDateTime, candidate.StartTimestamp.UtcDateTime);
        Assert.Equal(end.UtcDateTime, candidate.EndTimestamp.UtcDateTime);
        Assert.Equal(90, candidate.PeakValue);
        Assert.True(candidate.IsOpen);
    }

    [Fact]
    public void Constructor_WhenPeakValueIsNull_ShouldCreateCandidate()
    {
        var candidate = CreateCandidate(peakValue: null);

        Assert.Null(candidate.PeakValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenRuleIdIsNotPositive_ShouldThrowArgumentOutOfRangeException(long ruleId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCandidate(ruleId: ruleId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenDeviceIdIsBlank_ShouldThrowArgumentNullException(string? deviceId)
    {
        Assert.Throws<ArgumentNullException>(() => CreateCandidate(deviceId: deviceId!));
    }

    [Fact]
    public void Constructor_WhenMetricIsNull_ShouldThrowArgumentNullException()
    {
        var start = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentNullException>(() => new AlertCandidate(1, "PUMP-01", null!, start, start.AddSeconds(30), 80, false));
    }

    [Fact]
    public void Constructor_WhenEndIsBeforeStart_ShouldThrowArgumentException()
    {
        var start = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() => CreateCandidate(start: start, end: start.AddTicks(-1)));
    }

    [Fact]
    public void Constructor_WhenEndEqualsStart_ShouldCreateCandidate()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var candidate = CreateCandidate(start: timestamp, end: timestamp);

        Assert.Equal(candidate.StartTimestamp, candidate.EndTimestamp);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Constructor_WhenPeakValueIsNotFinite_ShouldThrowArgumentException(double peakValue)
    {
        Assert.Throws<ArgumentException>(() => CreateCandidate(peakValue: peakValue));
    }

    private static AlertCandidate CreateCandidate(long ruleId = 1, string deviceId = "PUMP-01", DateTimeOffset? start = null, DateTimeOffset? end = null, double? peakValue = 90)
    {
        var startTimestamp = start ?? DateTimeOffset.UtcNow;

        return new AlertCandidate(ruleId, deviceId, Metric.Temperature, startTimestamp, end ?? startTimestamp.AddSeconds(30), peakValue, false);
    }
}
