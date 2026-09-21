using SensorIngestion.Application.Abstractions.Rules.Evaluation.Stateful;
using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation.Stateful;

public sealed class SustainedAboveEvaluator : IStatefulRuleEvaluator
{
    public RuleOperator Operator { get; } = RuleOperator.Create(RuleOperatorNames.SustainedAbove);

    public StatefulRuleEvaluationResult Evaluate(IReadOnlyCollection<ReadingStream> streams, Rule rule)
    {
        ArgumentNullException.ThrowIfNull(streams);
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.Operator != Operator)
            throw new ArgumentException($"Evaluator '{Operator.Value}' cannot evaluate operator '{rule.Operator.Value}'.", nameof(rule));

        var decisions = new List<ReadingRuleEvaluationDecision>();
        var episodes = new List<RuleViolationEpisode>();

        foreach (var stream in streams)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var firstReading = stream.Readings.FirstOrDefault();
            if (firstReading is null)
                continue;

            if (rule.AppliesTo(firstReading))
                EvaluateStream(stream, rule, decisions, episodes);
        }

        return new StatefulRuleEvaluationResult(decisions, episodes);
    }

    private static void EvaluateStream(ReadingStream stream, Rule rule, ICollection<ReadingRuleEvaluationDecision> decisions, ICollection<RuleViolationEpisode> episodes)
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
                continue;
            }

            AddDecision(reading, rule, threshold, durationSeconds, isViolated: false, decisions);

            if (episodeStart is not null && isConfirmed)
            {
                episodes.Add(new RuleViolationEpisode(
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
            episodes.Add(new RuleViolationEpisode(
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

        var decision = new RuleEvaluationDecision(rule, isViolated, explanation);
        decisions.Add(new ReadingRuleEvaluationDecision(reading, decision));
    }

    private static double GetRequiredParameter(Rule rule, string parameterName)
    {
        var parameter = rule.Parameters.SingleOrDefault(x => string.Equals(x.Name, parameterName, StringComparison.OrdinalIgnoreCase));

        return parameter == null
            ? throw new InvalidOperationException($"Rule '{rule.RuleKey}' does not contain required parameter '{parameterName}'.")
            : parameter.Value;
    }
}
