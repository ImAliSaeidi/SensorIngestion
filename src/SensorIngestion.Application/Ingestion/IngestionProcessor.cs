using Microsoft.Extensions.Logging;
using SensorIngestion.Application.Abstractions.Ingestion;
using SensorIngestion.Application.Abstractions.Persistence;
using SensorIngestion.Application.Abstractions.Rules.Configuration;
using SensorIngestion.Application.Alerting;
using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Application.Persistence;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Domain.Alerts;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Ingestion;

public sealed class IngestionProcessor(
    IReadingSource readingSource,
    IReadingParser readingParser,
    IRuleConfigurationLoader ruleLoader,
    IRuleCatalog ruleCatalog,
    RuleEngine ruleEngine,
    AlertGenerator alertGenerator,
    IIngestionPersistence persistence,
    TimeProvider timeProvider,
    ILogger<IngestionProcessor> logger)
{
    public async Task<IngestionResult> ProcessAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readingSource);

        var startedAt = timeProvider.GetUtcNow();
        var fingerprint = await readingSource.GetFingerprintAsync(cancellationToken);

        // Read and validate the complete batch before evaluating it. This lets the
        // rule engine use event time even when the JSONL file is out of order.
        var rules = await LoadRulesAsync(startedAt, cancellationToken);
        var input = await ReadInputAsync(readingSource, cancellationToken);
        var deduplication = ReadingDeduplicator.Deduplicate(input.Readings);
        LogConflictingDuplicates(deduplication.Duplicates);

        var uniqueReadings = deduplication.UniqueReadings.ToArray();
        var evaluation = ruleEngine.Evaluate(uniqueReadings, rules);
        var evaluations = CreateEvaluationDrafts(evaluation);
        var alerts = GenerateAlerts(evaluation);

        var completedAt = timeProvider.GetUtcNow();
        var report = CreateReport(input, deduplication, evaluations, rules.Count, alerts.Alerts.Count);
        var request = new IngestionPersistenceRequest(fingerprint, startedAt, completedAt, uniqueReadings, evaluations.Drafts, alerts.Alerts, report);
        // Persistence owns the transaction and applies database-level idempotency.
        var persistenceResult = await persistence.PersistAsync(request, cancellationToken);
        LogCompleted(fingerprint, persistenceResult.Report);
        return new IngestionResult(persistenceResult.Report, input.Rejections, persistenceResult.PersistedAlerts);
    }

    #region Private Methods
    private async Task<IReadOnlyList<Rule>> LoadRulesAsync(DateTimeOffset loadedAt, CancellationToken cancellationToken)
    {
        var definitions = await ruleLoader.LoadAsync(cancellationToken);
        return await ruleCatalog.SynchronizeAsync(definitions, loadedAt, cancellationToken);
    }

    private async Task<InputBatch> ReadInputAsync(IReadingSource source, CancellationToken cancellationToken)
    {
        var readings = new List<SensorReading>();
        var rejections = new List<ReadingRejection>();
        var totalLines = 0;
        var parsedReadings = 0;

        await foreach (var line in source.ReadAsync(cancellationToken))
        {
            totalLines++;
            var parseResult = readingParser.Parse(line);

            if (parseResult.WasParsed)
                parsedReadings++;

            if (parseResult.IsSuccess)
            {
                readings.Add(parseResult.Reading!);
                continue;
            }

            var rejection = parseResult.Rejection!;
            rejections.Add(rejection);
            LogRejection(rejection);
        }

        return new InputBatch(readings, rejections, totalLines, parsedReadings);
    }

    private static EvaluationDraftBatch CreateEvaluationDrafts(RuleEngineResult evaluation)
    {
        var drafts = evaluation.Decisions
            .Select(x => new RuleEvaluationDraft(x.Reading, x.Decision.Rule, x.Decision.IsViolated, x.Decision.Explanation))
            .ToArray();

        return new EvaluationDraftBatch(drafts, evaluation.Decisions.Count(x => x.Decision.IsViolated));
    }

    private AlertGenerationResult GenerateAlerts(RuleEngineResult evaluation)
    {
        LogRuleViolationEpisodes(evaluation);

        var candidates = evaluation.Episodes
            .Select(episode => new AlertCandidate(episode.Rule.Id, episode.DeviceId, episode.Metric, episode.StartTimestamp, episode.EndTimestamp, episode.PeakValue, episode.IsOpen))
            .ToArray();
        var result = alertGenerator.Generate(candidates, timeProvider.GetUtcNow());
        LogAlertEvents(result);
        return result;
    }

    private static ProcessingReport CreateReport(InputBatch input, ReadingDeduplicationResult deduplication, EvaluationDraftBatch evaluations, int rulesLoaded, int alertsGenerated)
    {
        return new ProcessingReport(
            input.TotalLines,
            input.ParsedReadings,
            0,
            deduplication.Duplicates.Count,
            input.Rejections.Count,
            rulesLoaded,
            evaluations.Drafts.Count,
            deduplication.UniqueReadings.Count(x => x.Classification == ReadingClassification.Acceptable),
            deduplication.UniqueReadings.Count(x => x.Classification == ReadingClassification.Unacceptable),
            evaluations.ViolationCount,
            alertsGenerated);
    }

    private void LogRejection(ReadingRejection rejection)
        => logger.LogWarning(IngestionLogEvents.RejectedRecord, "Reading rejected at line {LineNumber} with category {Category}: {Reason}", rejection.LineNumber, rejection.Category, rejection.Reason);

    private void LogRuleViolationEpisodes(RuleEngineResult evaluation)
    {
        foreach (var episode in evaluation.Episodes)
        {
            logger.LogInformation(
                IngestionLogEvents.RuleViolationEpisode,
                "Rule violation episode confirmed for rule {RuleKey}, device {DeviceId}, metric {Metric}, from {StartTimestamp} to {EndTimestamp}",
                episode.Rule.RuleKey,
                episode.DeviceId,
                episode.Metric.Value,
                episode.StartTimestamp,
                episode.EndTimestamp);
        }
    }

    private void LogConflictingDuplicates(IEnumerable<DuplicateReading> duplicates)
    {
        foreach (var duplicate in duplicates.Where(x => x.HasConflictingValue))
        {
            logger.LogWarning(
                IngestionLogEvents.DuplicateConflict,
                "Conflicting duplicate kept first value for {DeviceId} {Metric} at {Timestamp} sequence {Sequence}",
                duplicate.KeptReading.DeviceId,
                duplicate.KeptReading.Metric.Value,
                duplicate.KeptReading.Timestamp,
                duplicate.KeptReading.Sequence);
        }
    }

    private void LogAlertEvents(AlertGenerationResult generated)
    {
        foreach (var alert in generated.Alerts)
            logger.LogInformation(IngestionLogEvents.AlertEmitted, "Alert emitted for rule {RuleId}, device {DeviceId}, metric {Metric}, starting {StartTimestamp}", alert.RuleId, alert.DeviceId, alert.Metric.Value, alert.StartTimestamp);

        foreach (var candidate in generated.SuppressedCandidates)
            logger.LogInformation(IngestionLogEvents.AlertSuppressed, "Alert suppressed by cooldown for rule {RuleId}, device {DeviceId}, metric {Metric}, starting {StartTimestamp}", candidate.RuleId, candidate.DeviceId, candidate.Metric.Value, candidate.StartTimestamp);
    }

    private void LogCompleted(string fingerprint, ProcessingReport report)
    {
        logger.LogInformation(
            IngestionLogEvents.IngestionCompleted,
            "Ingestion completed for fingerprint {FileFingerprint}: {TotalLinesRead} lines, {StoredReadings} stored readings, {InvalidRecordsRejected} invalid records, {AlertsGenerated} alerts",
            fingerprint,
            report.TotalLinesRead,
            report.StoredReadings,
            report.InvalidRecordsRejected,
            report.AlertsGenerated);
    }

    private sealed record InputBatch(IReadOnlyList<SensorReading> Readings, IReadOnlyList<ReadingRejection> Rejections, int TotalLines, int ParsedReadings);

    private sealed record EvaluationDraftBatch(IReadOnlyList<RuleEvaluationDraft> Drafts, int ViolationCount);
    #endregion
}
