using SensorIngestion.Domain.Alerts;

namespace SensorIngestion.Application.Alerting;

public sealed class AlertGenerationResult(IReadOnlyCollection<Alert> alerts, IReadOnlyCollection<AlertCandidate> suppressedCandidates)
{
    public IReadOnlyList<Alert> Alerts { get; } = alerts.ToList().AsReadOnly();

    public IReadOnlyList<AlertCandidate> SuppressedCandidates { get; } = suppressedCandidates.ToList().AsReadOnly();
}
