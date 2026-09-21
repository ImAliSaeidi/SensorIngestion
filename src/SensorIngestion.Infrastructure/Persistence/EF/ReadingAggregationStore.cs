using Microsoft.EntityFrameworkCore;
using SensorIngestion.Application.Abstractions.Aggregation;
using SensorIngestion.Application.Aggregation;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.Infrastructure.Persistence.EF;

public sealed class ReadingAggregationStore(SensorIngestionDbContext dbContext) : IReadingAggregationStore
{
    public async Task<IReadOnlyList<AcceptableReadingValue>> QueryAcceptableAsync(string deviceId, Metric metric, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        return await dbContext.Readings
            .AsNoTracking()
            .Where(reading =>
                reading.DeviceId == deviceId &&
                reading.Metric == metric &&
                reading.Classification == ReadingClassification.Acceptable &&
                reading.Timestamp >= from &&
                reading.Timestamp < to)
            .OrderBy(reading => reading.Timestamp)
            .Select(reading => new AcceptableReadingValue(reading.Timestamp, reading.Value))
            .ToListAsync(cancellationToken);
    }
}
