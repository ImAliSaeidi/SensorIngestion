namespace SensorIngestion.Application.Ingestion;

public sealed class InputLine
{
    public long LineNumber { get; }

    public string Content { get; }

    public InputLine(long lineNumber, string content)
    {
        if (lineNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(lineNumber), "input line number is required");

        ArgumentNullException.ThrowIfNull(content);

        LineNumber = lineNumber;
        Content = content;
    }
}
