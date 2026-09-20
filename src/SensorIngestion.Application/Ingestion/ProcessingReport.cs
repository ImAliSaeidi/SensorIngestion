namespace SensorIngestion.Application.Ingestion;

public sealed record ProcessingReport(
    int TotalLinesRead,
    int ParsedReadings,
    int StoredReadings,
    int DuplicatesRemoved,
    int InvalidRecordsRejected,
    int RulesLoaded,
    int RuleEvaluationsPerformed,
    int AcceptableReadings,
    int UnacceptableReadings,
    int RuleViolations,
    int AlertsGenerated);