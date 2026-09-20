using SensorIngestion.Application.Ingestion;
using System.Runtime.CompilerServices;

namespace SensorIngestion.Infrastructure.JsonLines;

public sealed class JsonlFileReadingSource : IReadingSource
{
    private readonly string _path;

    public JsonlFileReadingSource(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("input file path is required", nameof(path));

        _path = path;
    }

    public async IAsyncEnumerable<InputLine> ReadAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan
        };

        await using var stream = new FileStream(_path, options);
        using var reader = new StreamReader(stream);

        var lineNumber = 0L;

        while (await reader.ReadLineAsync(cancellationToken) is { } content)
            yield return new InputLine(++lineNumber, content);
    }
}
