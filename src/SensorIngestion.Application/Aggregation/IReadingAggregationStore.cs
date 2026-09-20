using SensorIngestion.Domain.Metrics;

namespace SensorIngestion.Application.Aggregation;

public interface IReadingAggregationStore
{
    Task<IReadOnlyList<AcceptableReadingValue>> QueryAcceptableAsync(string deviceId, Metric metric, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}
