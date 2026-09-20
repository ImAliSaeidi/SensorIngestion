using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Infrastructure.Persistence;

namespace SensorIngestion.IntegrationTests.Aggregation;

public sealed class EfReadingAggregationStoreTests
{
    private static readonly DateTimeOffset From = new(2025, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task QueryAcceptableAsync_ShouldApplyStreamClassificationAndHalfOpenRangeFiltersInDatabase()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new SensorIngestionDbContext(new DbContextOptionsBuilder<SensorIngestionDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();

        var includedAtFrom = Reading("PUMP-01", Metric.Temperature, From, 10, acceptable: true, sequence: 1);
        var includedBeforeTo = Reading("PUMP-01", Metric.Temperature, From.AddSeconds(119), 20, acceptable: true, sequence: 2);
        context.Readings.AddRange(
            includedAtFrom,
            includedBeforeTo,
            Reading("PUMP-01", Metric.Temperature, From.AddMinutes(2), 30, acceptable: true, sequence: 3),
            Reading("PUMP-01", Metric.Temperature, From.AddSeconds(30), 40, acceptable: false, sequence: 4),
            Reading("PUMP-02", Metric.Temperature, From.AddSeconds(30), 50, acceptable: true, sequence: 5),
            Reading("PUMP-01", Metric.Pressure, From.AddSeconds(30), 60, acceptable: true, sequence: 6));
        await context.SaveChangesAsync();

        var result = await new EfReadingAggregationStore(context).QueryAcceptableAsync("PUMP-01", Metric.Temperature, From, From.AddMinutes(2), CancellationToken.None);

        Assert.Collection(result,
            reading => Assert.Equal((includedAtFrom.Timestamp, includedAtFrom.Value), (reading.Timestamp, reading.Value)),
            reading => Assert.Equal((includedBeforeTo.Timestamp, includedBeforeTo.Value), (reading.Timestamp, reading.Value)));
    }

    private static SensorReading Reading(string deviceId, Metric metric, DateTimeOffset timestamp, double value, bool acceptable, long sequence)
    {
        var reading = new SensorReading(deviceId, metric, timestamp, value, sequence);
        reading.Classify(hasViolation: !acceptable);
        return reading;
    }
}
