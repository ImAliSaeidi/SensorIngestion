using SensorIngestion.Application.Alerting;
using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Alerts;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Application.Alerting;

public sealed class AlertGeneratorTests
{
    private static readonly DateTimeOffset Start = new(2025, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CandidateFactory_WhenEpisodeIsConfirmed_ShouldCopyEpisodeData()
    {
        var rule = CreateRule();
        var episode = new SustainedEpisode(rule, "PUMP-01", Metric.Temperature, Start, Start.AddSeconds(40), 92, false);

        var candidate = AlertCandidateFactory.Create(episode, persistedRuleId: 12);

        Assert.Equal(12, candidate.RuleId);
        Assert.Equal(episode.DeviceId, candidate.DeviceId);
        Assert.Equal(episode.Metric, candidate.Metric);
        Assert.Equal(episode.StartTimestamp, candidate.StartTimestamp);
        Assert.Equal(episode.EndTimestamp, candidate.EndTimestamp);
        Assert.Equal(episode.PeakValue, candidate.PeakValue);
        Assert.Equal(episode.IsOpen, candidate.IsOpen);
    }

    [Fact]
    public void Generate_WhenOneEpisodeHasManyViolatingReadings_ShouldCreateOneAlertForItsSingleCandidate()
    {
        var candidate = CreateCandidate(ruleId: 1, start: Start, end: Start.AddMinutes(1));

        var result = new AlertGenerator().Generate([candidate], Start.AddHours(1));

        Assert.Single(result.Alerts);
        Assert.Empty(result.SuppressedCandidates);
    }

    [Fact]
    public void Generate_WhenNextEpisodeStartsInsideCooldown_ShouldSuppressIt()
    {
        var first = CreateCandidate(ruleId: 1, start: Start, end: Start.AddMinutes(1));
        var second = CreateCandidate(ruleId: 1, start: Start.AddMinutes(5), end: Start.AddMinutes(6));

        var result = new AlertGenerator().Generate([first, second], Start.AddHours(1));

        Assert.Single(result.Alerts);
        Assert.Same(second, Assert.Single(result.SuppressedCandidates));
    }

    [Fact]
    public void Generate_WhenCooldownHasElapsedExactly_ShouldCreateNextAlert()
    {
        var first = CreateCandidate(ruleId: 1, start: Start, end: Start.AddMinutes(1));
        var second = CreateCandidate(ruleId: 1, start: Start.AddMinutes(6), end: Start.AddMinutes(7));

        var result = new AlertGenerator().Generate([first, second], Start.AddHours(1));

        Assert.Equal(2, result.Alerts.Count);
        Assert.Empty(result.SuppressedCandidates);
    }

    [Fact]
    public void Generate_WhenSuppressedEpisodeIsFollowedByAllowedEpisode_ShouldMeasureFromLastEmittedAlert()
    {
        var first = CreateCandidate(ruleId: 1, start: Start, end: Start.AddMinutes(1));
        var suppressed = CreateCandidate(ruleId: 1, start: Start.AddMinutes(2), end: Start.AddMinutes(10));
        var allowed = CreateCandidate(ruleId: 1, start: Start.AddMinutes(6), end: Start.AddMinutes(7));

        var result = new AlertGenerator().Generate([first, suppressed, allowed], Start.AddHours(1));

        Assert.Equal(2, result.Alerts.Count);
        Assert.Same(suppressed, Assert.Single(result.SuppressedCandidates));
    }

    [Fact]
    public void Generate_WhenAlertStreamsDiffer_ShouldTrackCooldownIndependently()
    {
        var candidates = new[]
        {
            CreateCandidate(ruleId: 1, deviceId: "PUMP-01", metric: Metric.Temperature, start: Start, end: Start.AddMinutes(1)),
            CreateCandidate(ruleId: 1, deviceId: "PUMP-02", metric: Metric.Temperature, start: Start.AddMinutes(2), end: Start.AddMinutes(3)),
            CreateCandidate(ruleId: 1, deviceId: "PUMP-01", metric: Metric.Pressure, start: Start.AddMinutes(2), end: Start.AddMinutes(3)),
            CreateCandidate(ruleId: 2, deviceId: "PUMP-01", metric: Metric.Temperature, start: Start.AddMinutes(2), end: Start.AddMinutes(3))
        };

        var result = new AlertGenerator().Generate(candidates, Start.AddHours(1));

        Assert.Equal(4, result.Alerts.Count);
        Assert.Empty(result.SuppressedCandidates);
    }

    [Fact]
    public void Identity_WhenNaturalKeyFieldsMatch_ShouldBeDeterministic()
    {
        var first = CreateCandidate(ruleId: 1, deviceId: "pump-01", start: Start, end: Start.AddMinutes(1));
        var second = CreateCandidate(ruleId: 1, deviceId: "PUMP-01", start: Start, end: Start.AddMinutes(2));

        Assert.Equal(first.Identity, second.Identity);
        Assert.Equal(first.Identity, Alert.Create(first, Start.AddHours(1)).Identity);
    }

    private static AlertCandidate CreateCandidate(long ruleId, DateTimeOffset start, DateTimeOffset end, string deviceId = "PUMP-01", Metric? metric = null)
        => new(ruleId, deviceId, metric ?? Metric.Temperature, start, end, 90, false);

    private static Rule CreateRule()
        => new(
            "sustained-temperature",
            1,
            "Sustained temperature",
            true,
            Metric.Temperature,
            null,
            RuleOperator.Create(RuleOperatorNames.SustainedAbove),
            [RuleParameter.Create(RuleParameterNames.Threshold, 80), RuleParameter.Create(RuleParameterNames.DurationSeconds, 30)],
            "hash",
            Start);
}
