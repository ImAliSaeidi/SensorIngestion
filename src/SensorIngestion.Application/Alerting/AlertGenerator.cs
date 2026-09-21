using SensorIngestion.Domain.Alerts;

namespace SensorIngestion.Application.Alerting;

public sealed class AlertGenerator
{
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(5);

    public AlertGenerationResult Generate(IEnumerable<AlertCandidate> candidates, DateTimeOffset createdAt, TimeSpan? cooldown = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var effectiveCooldown = cooldown ?? DefaultCooldown;
        if (effectiveCooldown < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(cooldown), "alert cooldown cannot be negative");

        var alerts = new List<Alert>();
        var suppressed = new List<AlertCandidate>();
        var lastEmittedEndByStream = new Dictionary<AlertStreamKey, DateTimeOffset>();

        // Cooldown is measured from the end of the last emitted episode. Suppressed
        // candidates do not move that reference point.
        foreach (var candidate in candidates.OrderBy(x => x.StartTimestamp).ThenBy(x => x.EndTimestamp))
        {
            ArgumentNullException.ThrowIfNull(candidate);

            var key = AlertStreamKey.From(candidate);
            if (lastEmittedEndByStream.TryGetValue(key, out var previousEnd) && candidate.StartTimestamp - previousEnd < effectiveCooldown)
            {
                suppressed.Add(candidate);
                continue;
            }

            alerts.Add(Alert.Create(candidate, createdAt));
            lastEmittedEndByStream[key] = candidate.EndTimestamp;
        }

        return new AlertGenerationResult(alerts, suppressed);
    }

    private sealed record AlertStreamKey(long RuleId, string DeviceId, string Metric)
    {
        public static AlertStreamKey From(AlertCandidate candidate)
            => new(candidate.RuleId, candidate.DeviceId.ToUpperInvariant(), candidate.Metric.Value);
    }
}
