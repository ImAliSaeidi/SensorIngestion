using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Rules.Evaluation.Stateful;

public sealed record SustainedEpisode(
    Rule Rule,
    string DeviceId,
    Metric Metric,
    DateTimeOffset StartTimestamp,
    DateTimeOffset EndTimestamp,
    double PeakValue,
    bool IsOpen);
