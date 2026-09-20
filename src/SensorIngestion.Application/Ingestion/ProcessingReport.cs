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
    int AlertsGenerated)
{
    public override string ToString()
        => $"Total lines read: {TotalLinesRead}{Environment.NewLine}" +
           $"Parsed readings: {ParsedReadings}{Environment.NewLine}" +
           $"Stored readings: {StoredReadings}{Environment.NewLine}" +
           $"Duplicates removed: {DuplicatesRemoved}{Environment.NewLine}" +
           $"Invalid records rejected: {InvalidRecordsRejected}{Environment.NewLine}" +
           $"Rules loaded: {RulesLoaded}{Environment.NewLine}" +
           $"Rule evaluations performed: {RuleEvaluationsPerformed}{Environment.NewLine}" +
           $"Acceptable readings: {AcceptableReadings}{Environment.NewLine}" +
           $"Unacceptable readings: {UnacceptableReadings}{Environment.NewLine}" +
           $"Rule violations: {RuleViolations}{Environment.NewLine}" +
           $"Alerts generated: {AlertsGenerated}";
}
