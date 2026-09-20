using SensorIngestion.Domain.Alerts;
using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.UnitTests.Domain.Alerts;

public sealed class AlertTests
{
    [Fact]
    public void Create_WhenCandidateIsValid_ShouldCopyCandidateValues()
    {
        var start = DateTimeOffset.UtcNow;
        var candidate = new AlertCandidate(1, "PUMP-01", Metric.Temperature, start, start.AddMinutes(1), 90, true);
        var createdAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.FromHours(3.5));

        var alert = Alert.Create(candidate, createdAt);

        Assert.Equal(candidate.RuleId, alert.RuleId);
        Assert.Equal(candidate.DeviceId, alert.DeviceId);
        Assert.Equal(candidate.Metric, alert.Metric);
        Assert.Equal(candidate.StartTimestamp, alert.StartTimestamp);
        Assert.Equal(candidate.EndTimestamp, alert.EndTimestamp);
        Assert.Equal(candidate.PeakValue, alert.PeakValue);
        Assert.Equal(candidate.IsOpen, alert.IsOpen);
        Assert.Equal(TimeSpan.Zero, alert.CreatedAt.Offset);
        Assert.Equal(createdAt.UtcDateTime, alert.CreatedAt.UtcDateTime);
    }

    [Fact]
    public void Create_WhenCandidateIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Alert.Create(null!, DateTimeOffset.UtcNow));
    }
}
