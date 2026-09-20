using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation.Stateful;

public sealed class SustainedAboveEvaluator
{
    public SustainedAboveEvaluationResult Evaluate(IReadOnlyCollection<ReadingStream> streams, IReadOnlyCollection<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(streams);
        ArgumentNullException.ThrowIfNull(rules);

        var decisions = new List<ReadingRuleEvaluationDecision>();
        var episodes = new List<SustainedEpisode>();
        var sustainedRules = rules.Where(IsSustainedAbove).ToArray();

        foreach (var stream in streams)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var firstReading = stream.Readings.FirstOrDefault();
            if (firstReading is null)
                continue;

            foreach (var rule in sustainedRules.Where(rule => rule.AppliesTo(firstReading)))
                EvaluateStream(stream, rule, decisions, episodes);
        }

        return new SustainedAboveEvaluationResult(decisions, episodes);
    }

    private static void EvaluateStream(ReadingStream stream, Rule rule, ICollection<ReadingRuleEvaluationDecision> decisions, ICollection<SustainedEpisode> episodes)
    {
        var threshold = GetRequiredParameter(rule, RuleParameterNames.Threshold);
        var durationSeconds = GetRequiredParameter(rule, RuleParameterNames.DurationSeconds);
        DateTimeOffset? episodeStart = null;
        var peakValue = double.MinValue;
        var isConfirmed = false;

        foreach (var reading in stream.Readings)
        {
            if (reading.Value > threshold)
            {
                if (episodeStart is null)
                {
                    episodeStart = reading.Timestamp;
                    peakValue = reading.Value;
                }
                else
                {
                    peakValue = Math.Max(peakValue, reading.Value);
                }

                var elapsedSeconds = (reading.Timestamp - episodeStart.Value).TotalSeconds;
                if (!isConfirmed && elapsedSeconds >= durationSeconds)
                    isConfirmed = true;

                AddDecision(reading, rule, threshold, durationSeconds, isConfirmed, decisions);
                ApplyClassification(reading, isConfirmed);
                continue;
            }

            AddDecision(reading, rule, threshold, durationSeconds, isViolated: false, decisions);
            ApplyClassification(reading, isViolated: false);

            if (episodeStart is not null && isConfirmed)
            {
                episodes.Add(new SustainedEpisode(
                    rule,
                    stream.DeviceId,
                    stream.Metric,
                    episodeStart.Value,
                    reading.Timestamp,
                    peakValue,
                    IsOpen: false));
            }

            episodeStart = null;
            peakValue = double.MinValue;
            isConfirmed = false;
        }

        if (episodeStart is not null && isConfirmed)
        {
            episodes.Add(new SustainedEpisode(
                rule,
                stream.DeviceId,
                stream.Metric,
                episodeStart.Value,
                stream.Readings[^1].Timestamp,
                peakValue,
                IsOpen: true));
        }
    }

    private static void AddDecision(SensorReading reading, Rule rule, double threshold, double durationSeconds, bool isViolated, ICollection<ReadingRuleEvaluationDecision> decisions)
    {
        var explanation = isViolated
            ? $"Value remained above threshold {threshold} for at least {durationSeconds} seconds."
            : null;

        var decision = new RuleEvaluationDecision(rule.RuleKey, rule.Name, rule.Operator, isViolated, explanation);
        decisions.Add(new ReadingRuleEvaluationDecision(reading, decision));
    }

    private static void ApplyClassification(SensorReading reading, bool isViolated)
    {
        if (isViolated)
        {
            reading.Classify(hasViolation: true);
            return;
        }

        if (reading.Classification == ReadingClassification.Pending)
            reading.Classify(hasViolation: false);
    }

    private static double GetRequiredParameter(Rule rule, string parameterName)
    {
        var parameter = rule.Parameters.SingleOrDefault(x => string.Equals(x.Name, parameterName, StringComparison.OrdinalIgnoreCase));

        return parameter == null
            ? throw new InvalidOperationException($"Rule '{rule.RuleKey}' does not contain required parameter '{parameterName}'.")
            : parameter.Value;
    }

    private static bool IsSustainedAbove(Rule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return string.Equals(rule.Operator.Value, RuleOperatorNames.SustainedAbove, StringComparison.Ordinal);
    }
}
