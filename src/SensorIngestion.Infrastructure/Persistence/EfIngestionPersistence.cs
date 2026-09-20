using Microsoft.EntityFrameworkCore;
using SensorIngestion.Application.Persistence;
using SensorIngestion.Domain.Alerts;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules.Evaluations;

namespace SensorIngestion.Infrastructure.Persistence;

public sealed class EfIngestionPersistence(SensorIngestionDbContext dbContext) : IIngestionPersistence
{
    public async Task<IngestionPersistenceResult> PersistAsync(IngestionPersistenceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var run = IngestionRunRecord.Start(request.FileFingerprint, request.StartedAt);
        dbContext.IngestionRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var storedReadings = await PersistReadingsAsync(request.Readings, cancellationToken);
            var storedEvaluations = await PersistEvaluationsAsync(request.Evaluations, cancellationToken);
            var storedAlerts = await PersistAlertsAsync(request.Alerts, cancellationToken);
            var effectiveReport = request.Report with { StoredReadings = storedReadings };

            run.Complete(request.CompletedAt, effectiveReport);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new IngestionPersistenceResult(storedReadings, storedEvaluations, storedAlerts, effectiveReport);
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.ChangeTracker.Clear();

            var failedRun = await dbContext.IngestionRuns.SingleAsync(x => x.Id == run.Id, CancellationToken.None);
            failedRun.Fail(DateTimeOffset.UtcNow, exception.Message);
            await dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<int> PersistReadingsAsync(IReadOnlyCollection<SensorReading> readings, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Readings.AsNoTracking().ToDictionaryAsync(x => x.Identity, cancellationToken);
        var newReadings = new List<SensorReading>();

        foreach (var reading in readings)
        {
            if (existing.ContainsKey(reading.Identity))
                continue;

            existing.Add(reading.Identity, reading);
            newReadings.Add(reading);
        }

        dbContext.Readings.AddRange(newReadings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return newReadings.Count;
    }

    private async Task<int> PersistEvaluationsAsync(IReadOnlyCollection<RuleEvaluationDraft> drafts, CancellationToken cancellationToken)
    {
        var readingIds = await dbContext.Readings.AsNoTracking().ToDictionaryAsync(x => x.Identity, x => x.Id, cancellationToken);
        var existing = await dbContext.RuleEvaluations.AsNoTracking().Select(x => new { x.SensorReadingId, x.RuleId }).ToListAsync(cancellationToken);
        var keys = existing.Select(x => (x.SensorReadingId, x.RuleId)).ToHashSet();
        var evaluations = new List<RuleEvaluation>();

        foreach (var draft in drafts)
        {
            if (!readingIds.TryGetValue(draft.Reading.Identity, out var readingId))
                throw new InvalidOperationException("evaluation references a reading that was not persisted");

            if (draft.Rule.Id <= 0)
                throw new InvalidOperationException($"rule '{draft.Rule.RuleKey}' must be persisted before its evaluations");

            if (!keys.Add((readingId, draft.Rule.Id)))
                continue;

            var evaluation = draft.IsViolated
                ? RuleEvaluation.Violated(readingId, draft.Rule.Id, draft.Explanation ?? "Rule was violated.", DateTimeOffset.UtcNow)
                : RuleEvaluation.Passed(readingId, draft.Rule.Id, DateTimeOffset.UtcNow);

            evaluations.Add(evaluation);
        }

        dbContext.RuleEvaluations.AddRange(evaluations);
        await dbContext.SaveChangesAsync(cancellationToken);
        return evaluations.Count;
    }

    private async Task<int> PersistAlertsAsync(IReadOnlyCollection<Alert> alerts, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Alerts.AsNoTracking().ToListAsync(cancellationToken);
        var identities = existing.Select(x => x.Identity).ToHashSet();
        var newAlerts = alerts.Where(x => identities.Add(x.Identity)).ToList();

        dbContext.Alerts.AddRange(newAlerts);
        await dbContext.SaveChangesAsync(cancellationToken);
        return newAlerts.Count;
    }
}
