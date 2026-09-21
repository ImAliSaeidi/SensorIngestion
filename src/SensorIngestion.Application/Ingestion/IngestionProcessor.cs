using Microsoft.Extensions.Logging;
using SensorIngestion.Application.Alerting;
using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Application.Persistence;
using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Configuration;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Ingestion;

public sealed class IngestionProcessor(
    IReadingSource defaultSource,
    IReadingParser parser,
    IRuleConfigurationLoader ruleLoader,
    IRuleCatalog ruleCatalog,
    StatelessRuleEvaluator statelessEvaluator,
    SustainedAboveEvaluator sustainedAboveEvaluator,
    AlertGenerator alertGenerator,
    IIngestionPersistence persistence,
    TimeProvider timeProvider,
    ILogger<IngestionProcessor> logger)
{
    public async Task<IngestionResult> ProcessAsync(CancellationToken cancellationToken)
        => await ProcessAsync(defaultSource, cancellationToken);

    public async Task<IngestionResult> ProcessAsync(IReadingSource source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        var startedAt = timeProvider.GetUtcNow();
        var fingerprint = await source.GetFingerprintAsync(cancellationToken);
        var definitions = await ruleLoader.LoadAsync(cancellationToken);
        var rules = await ruleCatalog.SynchronizeAsync(definitions, startedAt, cancellationToken);
        var readings = new List<SensorReading>();
        var rejections = new List<ReadingRejection>();
        var totalLines = 0;
        var parsedReadings = 0;

        await foreach (var line in source.ReadAsync(cancellationToken))
        {
            totalLines++;
            var parseResult = parser.Parse(line);

            if (parseResult.WasParsed)
                parsedReadings++;

            if (parseResult.IsSuccess)
            {
                readings.Add(parseResult.Reading!);
                continue;
            }

            var rejection = parseResult.Rejection!;
            rejections.Add(rejection);
            logger.LogWarning(IngestionLogEvents.RejectedRecord, "Reading rejected at line {LineNumber} with category {Category}: {Reason}", rejection.LineNumber, rejection.Category, rejection.Reason);
        }

        var deduplication = ReadingDeduplicator.Deduplicate(readings);
        LogConflictingDuplicates(deduplication.Duplicates);

        var uniqueReadings = deduplication.UniqueReadings.ToArray();
        var statelessRules = rules.Where(rule => !IsSustainedAbove(rule)).ToArray();
        var evaluationDrafts = new List<RuleEvaluationDraft>();
        var violationCount = 0;

        foreach (var reading in uniqueReadings)
        {
            var evaluation = statelessEvaluator.Evaluate(reading, statelessRules);
            foreach (var decision in evaluation.Decisions)
                AddDraft(reading, decision, rules, evaluationDrafts, ref violationCount);
        }

        var streams = ReadingStreamOrganizer.Organize(uniqueReadings);
        var sustainedResult = sustainedAboveEvaluator.Evaluate(streams, rules);

        foreach (var readingDecision in sustainedResult.Decisions)
            AddDraft(readingDecision.Reading, readingDecision.Decision, rules, evaluationDrafts, ref violationCount);

        foreach (var episode in sustainedResult.Episodes)
        {
            logger.LogInformation(
                IngestionLogEvents.SustainedEpisode,
                "Sustained episode confirmed for rule {RuleKey}, device {DeviceId}, metric {Metric}, from {StartTimestamp} to {EndTimestamp}",
                episode.Rule.RuleKey,
                episode.DeviceId,
                episode.Metric.Value,
                episode.StartTimestamp,
                episode.EndTimestamp);
        }

        var candidates = sustainedResult.Episodes.Select(episode => AlertCandidateFactory.Create(episode, episode.Rule.Id)).ToArray();
        var generated = alertGenerator.Generate(candidates, timeProvider.GetUtcNow());
        LogAlertEvents(generated);

        var completedAt = timeProvider.GetUtcNow();
        var report = new ProcessingReport(
            totalLines,
            parsedReadings,
            0,
            deduplication.Duplicates.Count,
            rejections.Count,
            rules.Count,
            evaluationDrafts.Count,
            uniqueReadings.Count(x => x.Classification == ReadingClassification.Acceptable),
            uniqueReadings.Count(x => x.Classification == ReadingClassification.Unacceptable),
            violationCount,
            generated.Alerts.Count);

        var request = new IngestionPersistenceRequest(fingerprint, startedAt, completedAt, uniqueReadings, evaluationDrafts, generated.Alerts, report);
        var persistenceResult = await persistence.PersistAsync(request, cancellationToken);

        logger.LogInformation(
            IngestionLogEvents.IngestionCompleted,
            "Ingestion completed for fingerprint {FileFingerprint}: {TotalLinesRead} lines, {StoredReadings} stored readings, {InvalidRecordsRejected} invalid records, {AlertsGenerated} alerts",
            fingerprint,
            persistenceResult.Report.TotalLinesRead,
            persistenceResult.Report.StoredReadings,
            persistenceResult.Report.InvalidRecordsRejected,
            persistenceResult.Report.AlertsGenerated);

        return new IngestionResult(persistenceResult.Report, rejections, persistenceResult.PersistedAlerts);
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

    private static void AddDraft(SensorReading reading, RuleEvaluationDecision decision, IReadOnlyCollection<Rule> rules, ICollection<RuleEvaluationDraft> drafts, ref int violationCount)
    {
        var rule = rules.Single(x => x.RuleKey == decision.RuleKey);
        drafts.Add(new RuleEvaluationDraft(reading, rule, decision.IsViolated, decision.Explanation));

        if (decision.IsViolated)
            violationCount++;
    }

    private static bool IsSustainedAbove(Rule rule)
        => string.Equals(rule.Operator.Value, RuleOperatorNames.SustainedAbove, StringComparison.Ordinal);
}
