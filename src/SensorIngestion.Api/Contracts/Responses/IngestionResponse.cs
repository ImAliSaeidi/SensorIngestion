namespace SensorIngestion.Api.Contracts.Responses;

public sealed record IngestionResponse(
    ProcessingReportResponse Report,
    IReadOnlyList<ReadingRejectionResponse> Rejections,
    IReadOnlyList<AlertResponse> Alerts);

public sealed record ProcessingReportResponse(
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

public sealed record ReadingRejectionResponse(long LineNumber, string Category, string Reason, string? FieldName);

public sealed record AlertResponse(
    long Id,
    long RuleId,
    string DeviceId,
    string Metric,
    DateTimeOffset StartTimestamp,
    DateTimeOffset EndTimestamp,
    double? PeakValue,
    bool IsOpen,
    DateTimeOffset CreatedAt);
