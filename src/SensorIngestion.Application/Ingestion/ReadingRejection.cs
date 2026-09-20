namespace SensorIngestion.Application.Ingestion;

public sealed record ReadingRejection
{
    public long LineNumber { get; }

    public RejectionCategory Category { get; }

    public string Reason { get; }

    public string? FieldName { get; }

    public ReadingRejection(long lineNumber, RejectionCategory category, string reason, string? fieldName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.", nameof(reason));

        LineNumber = lineNumber;
        Category = category;
        Reason = reason.Trim();
        FieldName = string.IsNullOrWhiteSpace(fieldName)
            ? null
            : fieldName.Trim();
    }
}
