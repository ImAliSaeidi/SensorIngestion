using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Application.RuleEvaluation.Stateful;

public sealed class SustainedAboveEvaluatorTests
{
    private static readonly DateTimeOffset Start = new(2025, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_WhenValueEqualsThreshold_ShouldNotStartEpisode()
    {
        var readings = new[] { CreateReading(0, 80), CreateReading(30, 80) };

        var result = Evaluate(readings, [CreateRule(threshold: 80, durationSeconds: 30)]);

        Assert.Empty(result.Episodes);
        Assert.All(result.Decisions, decision => Assert.False(decision.Decision.IsViolated));
        Assert.All(readings, reading => Assert.Equal(ReadingClassification.Acceptable, reading.Classification));
    }

    [Fact]
    public void Evaluate_WhenCandidateEndsBeforeRequiredDuration_ShouldDiscardCandidate()
    {
        var readings = new[] { CreateReading(0, 81), CreateReading(20, 85), CreateReading(29, 80) };

        var result = Evaluate(readings, [CreateRule(threshold: 80, durationSeconds: 30)]);

        Assert.Empty(result.Episodes);
        Assert.All(result.Decisions, decision => Assert.False(decision.Decision.IsViolated));
        Assert.All(readings, reading => Assert.Equal(ReadingClassification.Acceptable, reading.Classification));
    }

    [Fact]
    public void Evaluate_WhenDurationBoundaryIsReachedExactly_ShouldConfirmAndCloseEpisode()
    {
        var readings = new[] { CreateReading(0, 81), CreateReading(30, 85), CreateReading(40, 80) };
        var rule = CreateRule(threshold: 80, durationSeconds: 30);

        var result = Evaluate(readings, [rule]);

        var episode = Assert.Single(result.Episodes);
        Assert.Same(rule, episode.Rule);
        Assert.Equal(Start, episode.StartTimestamp);
        Assert.Equal(Start.AddSeconds(40), episode.EndTimestamp);
        Assert.Equal(85, episode.PeakValue);
        Assert.False(episode.IsOpen);
        Assert.Equal(ReadingClassification.Acceptable, readings[0].Classification);
        Assert.Equal(ReadingClassification.Unacceptable, readings[1].Classification);
        Assert.Equal(ReadingClassification.Acceptable, readings[2].Classification);
        var violation = Assert.Single(result.Decisions, x => x.Decision.IsViolated);
        Assert.Same(readings[1], violation.Reading);
        Assert.False(string.IsNullOrWhiteSpace(violation.Decision.Explanation));
    }

    [Fact]
    public void Evaluate_AfterConfirmation_ShouldClassifyAboveThresholdReadingsUntilEpisodeCloses()
    {
        var readings = new[] { CreateReading(0, 81), CreateReading(31, 82), CreateReading(40, 90), CreateReading(50, 80) };

        var result = Evaluate(readings, [CreateRule(threshold: 80, durationSeconds: 30)]);

        Assert.Equal(ReadingClassification.Acceptable, readings[0].Classification);
        Assert.Equal(ReadingClassification.Unacceptable, readings[1].Classification);
        Assert.Equal(ReadingClassification.Unacceptable, readings[2].Classification);
        Assert.Equal(ReadingClassification.Acceptable, readings[3].Classification);
        Assert.Equal(2, result.Decisions.Count(x => x.Decision.IsViolated));
    }

    [Fact]
    public void Evaluate_WhenConfirmedEpisodeRemainsOpen_ShouldFinalizeAtLastObservedTimestamp()
    {
        var readings = new[] { CreateReading(0, 81), CreateReading(30, 85), CreateReading(45, 90) };

        var result = Evaluate(readings, [CreateRule(threshold: 80, durationSeconds: 30)]);

        var episode = Assert.Single(result.Episodes);
        Assert.Equal(Start, episode.StartTimestamp);
        Assert.Equal(Start.AddSeconds(45), episode.EndTimestamp);
        Assert.Equal(90, episode.PeakValue);
        Assert.True(episode.IsOpen);
    }

    [Fact]
    public void Evaluate_WhenUnconfirmedCandidateRemainsOpen_ShouldDiscardCandidate()
    {
        var readings = new[] { CreateReading(0, 81), CreateReading(29, 90) };

        var result = Evaluate(readings, [CreateRule(threshold: 80, durationSeconds: 30)]);

        Assert.Empty(result.Episodes);
        Assert.All(readings, reading => Assert.Equal(ReadingClassification.Acceptable, reading.Classification));
    }

    [Fact]
    public void Evaluate_WhenInputOrderIsShuffled_ShouldProduceSameEventTimeResultAsSortedInput()
    {
        var sortedReadings = new[] { CreateReading(0, 81), CreateReading(10, 85), CreateReading(30, 83), CreateReading(45, 79) };
        var shuffledReadings = new[] { CreateReading(30, 83), CreateReading(45, 79), CreateReading(0, 81), CreateReading(10, 85) };
        var rule = CreateRule(threshold: 80, durationSeconds: 30);

        var sortedResult = Evaluate(sortedReadings, [rule]);
        var shuffledResult = Evaluate(shuffledReadings, [rule]);

        var sortedEpisode = Assert.Single(sortedResult.Episodes);
        var shuffledEpisode = Assert.Single(shuffledResult.Episodes);
        Assert.Equal(sortedEpisode.StartTimestamp, shuffledEpisode.StartTimestamp);
        Assert.Equal(sortedEpisode.EndTimestamp, shuffledEpisode.EndTimestamp);
        Assert.Equal(sortedEpisode.PeakValue, shuffledEpisode.PeakValue);
        Assert.Equal(
            sortedReadings.OrderBy(x => x.Timestamp).Select(x => x.Classification),
            shuffledReadings.OrderBy(x => x.Timestamp).Select(x => x.Classification));
    }

    [Fact]
    public void Evaluate_WhenStreamsAndRulesAreInterleaved_ShouldKeepStateIsolated()
    {
        var pump1Temperature = new[] { CreateReading(0, 81, "PUMP-01"), CreateReading(30, 82, "PUMP-01") };
        var pump2Temperature = new[] { CreateReading(0, 95, "PUMP-02"), CreateReading(10, 96, "PUMP-02") };
        var pump1Pressure = new[] { CreateReading(0, 6, "PUMP-01", Metric.Pressure), CreateReading(20, 7, "PUMP-01", Metric.Pressure) };
        var readings = pump2Temperature.Take(1).Concat(pump1Pressure.Take(1)).Concat(pump1Temperature).Concat(pump2Temperature.Skip(1)).Concat(pump1Pressure.Skip(1)).ToArray();
        var globalTemperatureRule = CreateRule("global-temperature", threshold: 80, durationSeconds: 30);
        var pump2TemperatureRule = CreateRule("pump2-temperature", deviceId: "PUMP-02", threshold: 90, durationSeconds: 10);
        var pressureRule = CreateRule("global-pressure", metric: Metric.Pressure, threshold: 5, durationSeconds: 20);

        var result = Evaluate(readings, [globalTemperatureRule, pump2TemperatureRule, pressureRule]);

        Assert.Equal(3, result.Episodes.Count);
        Assert.Contains(result.Episodes, x => ReferenceEquals(x.Rule, globalTemperatureRule) && x.DeviceId == "PUMP-01");
        Assert.DoesNotContain(result.Episodes, x => ReferenceEquals(x.Rule, globalTemperatureRule) && x.DeviceId == "PUMP-02");
        Assert.Contains(result.Episodes, x => ReferenceEquals(x.Rule, pump2TemperatureRule) && x.DeviceId == "PUMP-02");
        Assert.Contains(result.Episodes, x => ReferenceEquals(x.Rule, pressureRule) && x.Metric == Metric.Pressure);
    }

    [Fact]
    public void Evaluate_WhenReadingAlreadyHasStatelessViolation_ShouldNotOverwriteItWithPassingStatefulDecision()
    {
        var readings = new[] { CreateReading(0, 81), CreateReading(30, 85) };
        readings[0].Classify(hasViolation: true);
        readings[1].Classify(hasViolation: false);
        var streams = ReadingStreamOrganizer.Organize(readings);

        new SustainedAboveEvaluator().Evaluate(streams, [CreateRule(threshold: 80, durationSeconds: 30)]);

        Assert.Equal(ReadingClassification.Unacceptable, readings[0].Classification);
        Assert.Equal(ReadingClassification.Unacceptable, readings[1].Classification);
    }

    private static SustainedAboveEvaluationResult Evaluate(IReadOnlyCollection<SensorReading> readings, IReadOnlyCollection<Rule> rules)
    {
        foreach (var reading in readings.Where(x => x.Classification == ReadingClassification.Pending))
            reading.Classify(hasViolation: false);

        var streams = ReadingStreamOrganizer.Organize(readings);
        return new SustainedAboveEvaluator().Evaluate(streams, rules);
    }

    private static SensorReading CreateReading(int seconds, double value, string deviceId = "PUMP-01", Metric? metric = null)
    {
        return new SensorReading(deviceId, metric ?? Metric.Temperature, Start.AddSeconds(seconds), value, sequence: seconds);
    }

    private static Rule CreateRule(string ruleKey = "sustained-temperature", string? deviceId = null, Metric? metric = null, double threshold = 80, double durationSeconds = 30)
    {
        return new Rule(
            ruleKey,
            1,
            ruleKey,
            true,
            metric ?? Metric.Temperature,
            deviceId,
            RuleOperator.Create(RuleOperatorNames.SustainedAbove),
            [RuleParameter.Create(RuleParameterNames.Threshold, threshold), RuleParameter.Create(RuleParameterNames.DurationSeconds, durationSeconds)],
            $"hash-{ruleKey}",
            Start);
    }
}
