namespace SensorIngestion.Application.Ingestion;

public sealed class InputLine
{
    public long LineNumber { get; }

    public string Content { get; }

    public InputLine(long lineNumber, string content)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lineNumber);

        ArgumentNullException.ThrowIfNull(content);

        LineNumber = lineNumber;
        Content = content;
    }
}
