using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SensorIngestion.Application.Ingestion;
using SensorIngestion.Application.Persistence;
using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Domain.Alerts;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;
using SensorIngestion.Infrastructure.Persistence.EF;

namespace SensorIngestion.IntegrationTests.Persistence;

public sealed class SqlitePersistenceTests
{
    private static readonly DateTimeOffset Start = new(2025, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RuleCatalog_WhenConfigurationIsRepeatedOrChanged_ShouldReuseOrVersionRules()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var catalog = new RuleCatalog(fixture.Context);
        var original = CreateDefinition("hash-1", threshold: 80);

        var first = Assert.Single(await catalog.SynchronizeAsync([original], Start, CancellationToken.None));
        var repeated = Assert.Single(await catalog.SynchronizeAsync([original], Start.AddMinutes(1), CancellationToken.None));
        var changed = Assert.Single(await catalog.SynchronizeAsync([CreateDefinition("hash-2", threshold: 85)], Start.AddMinutes(2), CancellationToken.None));

        Assert.Equal(first.Id, repeated.Id);
        Assert.Equal(1, first.Version);
        Assert.Equal(2, changed.Version);
        Assert.Equal(2, await fixture.Context.Rules.CountAsync());
    }

    [Fact]
    public async Task Persist_WhenSameBatchIsProcessedTwice_ShouldNotDuplicateRecords()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var rule = await PersistRuleAsync(fixture.Context);
        var reading = new SensorReading("PUMP-01", Metric.Temperature, Start, 90, 1);
        reading.Classify(hasViolation: true);
        var evaluation = new RuleEvaluationDraft(reading, rule, true, "Temperature exceeded threshold.");
        var alert = Alert.Create(new AlertCandidate(rule.Id, reading.DeviceId, reading.Metric, Start, Start.AddMinutes(1), 90, false), Start.AddHours(1));
        var persistence = new IngestionPersistence(fixture.Context);

        var first = await persistence.PersistAsync(CreateRequest("fingerprint", [reading], [evaluation], [alert]), CancellationToken.None);
        var second = await persistence.PersistAsync(CreateRequest("fingerprint", [reading], [evaluation], [alert]), CancellationToken.None);

        Assert.Equal(1, first.StoredReadings);
        Assert.Equal(1, first.StoredEvaluations);
        Assert.Equal(1, first.StoredAlerts);
        Assert.Equal(0, second.StoredReadings);
        Assert.Equal(0, second.StoredEvaluations);
        Assert.Equal(0, second.StoredAlerts);
        Assert.Equal(1, await fixture.Context.Readings.CountAsync());
        Assert.Equal(1, await fixture.Context.RuleEvaluations.CountAsync());
        Assert.Equal(1, await fixture.Context.Alerts.CountAsync());
        Assert.Equal(2, await fixture.Context.IngestionRuns.CountAsync(x => x.Status == "Completed"));
    }

    [Fact]
    public async Task Persist_WhenAWriteFails_ShouldRollbackProcessedDataAndRecordFailedRun()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var reading = new SensorReading("PUMP-01", Metric.Temperature, Start, 90, 1);
        reading.Classify(hasViolation: false);
        var invalidAlert = Alert.Create(new AlertCandidate(999, reading.DeviceId, reading.Metric, Start, Start.AddMinutes(1), 90, false), Start.AddHours(1));
        var persistence = new IngestionPersistence(fixture.Context);

        await Assert.ThrowsAsync<DbUpdateException>(() => persistence.PersistAsync(CreateRequest("failed", [reading], [], [invalidAlert]), CancellationToken.None));

        Assert.Empty(await fixture.Context.Readings.AsNoTracking().ToListAsync());
        Assert.Empty(await fixture.Context.Alerts.AsNoTracking().ToListAsync());
        var failedRun = Assert.Single(await fixture.Context.IngestionRuns.AsNoTracking().ToListAsync());
        Assert.Equal("Failed", failedRun.Status);
        Assert.False(string.IsNullOrWhiteSpace(failedRun.FailureReason));
    }

    [Fact]
    public async Task Model_ShouldHaveRequiredUniqueAndAggregationIndexes()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var readingIndexes = fixture.Context.Model.FindEntityType(typeof(SensorReading))!.GetIndexes().ToList();
        var alertIndexes = fixture.Context.Model.FindEntityType(typeof(Alert))!.GetIndexes().ToList();

        Assert.Contains(readingIndexes, x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(["DeviceId", "Metric", "Timestamp", "Sequence"]));
        Assert.Contains(readingIndexes, x => x.Properties.Select(p => p.Name).SequenceEqual(["DeviceId", "Metric", "Classification", "Timestamp"]));
        Assert.Contains(alertIndexes, x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(["RuleId", "DeviceId", "Metric", "StartTimestamp"]));
    }

    private static async Task<Rule> PersistRuleAsync(SensorIngestionDbContext context)
    {
        var catalog = new RuleCatalog(context);
        return Assert.Single(await catalog.SynchronizeAsync([CreateDefinition("hash-1", 80)], Start, CancellationToken.None));
    }

    private static RuleDefinition CreateDefinition(string hash, double threshold)
        => new(
            "temperature-limit",
            "Temperature limit",
            true,
            Metric.Temperature,
            null,
            RuleOperator.Create(RuleOperatorNames.GreaterThan),
            [RuleParameter.Create(RuleParameterNames.Threshold, threshold)],
            hash);

    private static IngestionPersistenceRequest CreateRequest(string fingerprint, IReadOnlyCollection<SensorReading> readings, IReadOnlyCollection<RuleEvaluationDraft> evaluations, IReadOnlyCollection<Alert> alerts)
        => new(fingerprint, Start, Start.AddHours(1), readings, evaluations, alerts, new ProcessingReport(1, 1, 0, 0, 0, 1, evaluations.Count, readings.Count(x => x.Classification == ReadingClassification.Acceptable), readings.Count(x => x.Classification == ReadingClassification.Unacceptable), evaluations.Count(x => x.IsViolated), alerts.Count));

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public SensorIngestionDbContext Context { get; }

        private SqliteFixture(SqliteConnection connection, SensorIngestionDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public static async Task<SqliteFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SensorIngestionDbContext>().UseSqlite(connection).Options;
            var context = new SensorIngestionDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new SqliteFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
