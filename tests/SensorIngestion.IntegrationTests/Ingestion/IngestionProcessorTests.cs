using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using SensorIngestion.Application.Alerting;
using SensorIngestion.Application.Ingestion;
using SensorIngestion.Application.Persistence;
using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Application.Rules.Evaluation.Operators;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Rules;
using SensorIngestion.Infrastructure.JsonLines;
using SensorIngestion.Infrastructure.Persistence;
using System.Runtime.CompilerServices;

namespace SensorIngestion.IntegrationTests.Ingestion;

public sealed class IngestionProcessorTests
{
    private static readonly DateTimeOffset Start = new(2025, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_WithMessyOutOfOrderInput_ShouldProduceExpectedReportAndPersistIdempotently()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var processor = CreateProcessor(fixture.Context, new TestReadingSource(CreateLines()), new TestRuleLoader(CreateDefinitions()));

        var first = await processor.ProcessAsync(CancellationToken.None);
        var second = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(10, first.Report.TotalLinesRead);
        Assert.Equal(9, first.Report.ParsedReadings);
        Assert.Equal(8, first.Report.StoredReadings);
        Assert.Equal(1, first.Report.DuplicatesRemoved);
        Assert.Equal(1, first.Report.InvalidRecordsRejected);
        Assert.Equal(2, first.Report.RulesLoaded);
        Assert.Equal(14, first.Report.RuleEvaluationsPerformed);
        Assert.Equal(6, first.Report.AcceptableReadings);
        Assert.Equal(2, first.Report.UnacceptableReadings);
        Assert.Equal(2, first.Report.RuleViolations);
        Assert.Equal(1, first.Report.AlertsGenerated);
        Assert.Equal(0, second.Report.StoredReadings);
        Assert.Equal(0, second.Report.AlertsGenerated);
        Assert.Empty(second.Alerts);
        Assert.Equal(8, await fixture.Context.Readings.CountAsync());
        Assert.Equal(14, await fixture.Context.RuleEvaluations.CountAsync());
        Assert.Equal(1, await fixture.Context.Alerts.CountAsync());
        Assert.Equal(2, await fixture.Context.IngestionRuns.CountAsync(x => x.Status == "Completed"));
    }

    [Fact]
    public async Task ProcessAsync_WhenCancellationIsRequested_ShouldStopBeforePersistence()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var source = new TestReadingSource(CreateLines());
        var processor = CreateProcessor(fixture.Context, source, new TestRuleLoader(CreateDefinitions()));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => processor.ProcessAsync(cancellation.Token));

        Assert.Empty(await fixture.Context.IngestionRuns.ToListAsync());
        Assert.Empty(await fixture.Context.Readings.ToListAsync());
    }

    [Fact]
    public async Task ProcessAsync_WhenPersistenceFails_ShouldPropagateFailure()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var processor = CreateProcessor(fixture.Context, new TestReadingSource(CreateLines()), new TestRuleLoader(CreateDefinitions()), new ThrowingPersistence());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessAsync(CancellationToken.None));

        Assert.Equal("Persistence failed.", exception.Message);
    }

    [Fact]
    public async Task ProcessAsync_ShouldWriteFocusedStructuredOperationalLogs()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var logger = new RecordingLogger<IngestionProcessor>();
        var processor = CreateProcessor(fixture.Context, new TestReadingSource(CreateLines()), new TestRuleLoader(CreateDefinitions()), logger: logger);

        await processor.ProcessAsync(CancellationToken.None);

        Assert.Contains(logger.Entries, x => x.EventId == IngestionLogEvents.RejectedRecord.Id && x.HasProperty("LineNumber"));
        Assert.Contains(logger.Entries, x => x.EventId == IngestionLogEvents.DuplicateConflict.Id && x.HasProperty("DeviceId") && x.HasProperty("Sequence"));
        Assert.Equal(2, logger.Entries.Count(x => x.EventId == IngestionLogEvents.SustainedEpisode.Id));
        Assert.Contains(logger.Entries, x => x.EventId == IngestionLogEvents.AlertEmitted.Id && x.HasProperty("RuleId"));
        Assert.Contains(logger.Entries, x => x.EventId == IngestionLogEvents.AlertSuppressed.Id && x.HasProperty("StartTimestamp"));
        Assert.Contains(logger.Entries, x => x.EventId == IngestionLogEvents.IngestionCompleted.Id && x.HasProperty("FileFingerprint") && x.HasProperty("StoredReadings"));
    }

    private static IngestionProcessor CreateProcessor(SensorIngestionDbContext context, IReadingSource source, IRuleConfigurationLoader ruleLoader, IIngestionPersistence? persistence = null, ILogger<IngestionProcessor>? logger = null)
    {
        var registry = new RuleOperatorRegistry([new GreaterThanOperatorStrategy()]);
        return new IngestionProcessor(
            source,
            new JsonlReadingParser(),
            ruleLoader,
            new EfRuleCatalog(context),
            new StatelessRuleEvaluator(registry),
            new SustainedAboveEvaluator(),
            new AlertGenerator(),
            persistence ?? new EfIngestionPersistence(context),
            new FixedTimeProvider(Start.AddHours(2)),
            logger ?? NullLogger<IngestionProcessor>.Instance);
    }

    private static IReadOnlyList<InputLine> CreateLines()
        =>
        [
            Line(1, "temperature", "2025-06-01T08:00:30Z", 85, 3),
            new InputLine(2, "not-json"),
            Line(3, "temperature", "2025-06-01T08:00:00Z", 81, 1),
            Line(4, "temperature", "2025-06-01T08:00:00Z", 99, 1),
            Line(5, "temperature", "2025-06-01T08:00:40Z", 80, 4),
            Line(6, "temperature", "2025-06-01T08:00:10Z", 82, 2),
            Line(7, "temperature", "2025-06-01T08:01:00Z", 81, 5),
            Line(8, "temperature", "2025-06-01T08:01:30Z", 85, 6),
            Line(9, "temperature", "2025-06-01T08:01:40Z", 80, 7),
            Line(10, "pressure", "2025-06-01T08:00:00Z", 4, 1)
        ];

    private static InputLine Line(long lineNumber, string metric, string timestamp, double value, long sequence)
        => new(lineNumber, $"{{\"deviceId\":\"PUMP-01\",\"metric\":\"{metric}\",\"ts\":\"{timestamp}\",\"value\":{value},\"seq\":{sequence}}}");

    private static IReadOnlyList<RuleDefinition> CreateDefinitions()
        =>
        [
            new RuleDefinition(
                "positive-temperature",
                "Positive temperature",
                true,
                Metric.Temperature,
                null,
                RuleOperator.Create(RuleOperatorNames.GreaterThan),
                [RuleParameter.Create(RuleParameterNames.Threshold, 0)],
                "hash-stateless"),
            new RuleDefinition(
                "sustained-temperature",
                "Sustained temperature",
                true,
                Metric.Temperature,
                "PUMP-01",
                RuleOperator.Create(RuleOperatorNames.SustainedAbove),
                [RuleParameter.Create(RuleParameterNames.Threshold, 80), RuleParameter.Create(RuleParameterNames.DurationSeconds, 30)],
                "hash-sustained")
        ];

    private sealed class TestReadingSource(IReadOnlyList<InputLine> lines) : IReadingSource
    {
        public ValueTask<string> GetFingerprintAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult("fixture-fingerprint");
        }

        public async IAsyncEnumerable<InputLine> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var line in lines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return line;
                await Task.Yield();
            }
        }
    }

    private sealed class TestRuleLoader(IReadOnlyList<RuleDefinition> definitions) : IRuleConfigurationLoader
    {
        public Task<IReadOnlyList<RuleDefinition>> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(definitions);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;
    }

    private sealed class ThrowingPersistence : IIngestionPersistence
    {
        public Task<IngestionPersistenceResult> PersistAsync(IngestionPersistenceRequest request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Persistence failed.");
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(x => x.Key, x => x.Value)
                : new Dictionary<string, object?>();

            Entries.Add(new LogEntry(logLevel, eventId.Id, properties));
        }
    }

    private sealed record LogEntry(LogLevel Level, int EventId, IReadOnlyDictionary<string, object?> Properties)
    {
        public bool HasProperty(string name) => Properties.ContainsKey(name);
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose() { }
    }

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
            var context = new SensorIngestionDbContext(new DbContextOptionsBuilder<SensorIngestionDbContext>().UseSqlite(connection).Options);
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
