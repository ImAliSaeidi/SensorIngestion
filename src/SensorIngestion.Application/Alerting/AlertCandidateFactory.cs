using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Alerts;

namespace SensorIngestion.Application.Alerting;

public static class AlertCandidateFactory
{
    public static AlertCandidate Create(SustainedEpisode episode, long persistedRuleId)
    {
        ArgumentNullException.ThrowIfNull(episode);

        return new AlertCandidate(
            persistedRuleId,
            episode.DeviceId,
            episode.Metric,
            episode.StartTimestamp,
            episode.EndTimestamp,
            episode.PeakValue,
            episode.IsOpen);
    }
}
