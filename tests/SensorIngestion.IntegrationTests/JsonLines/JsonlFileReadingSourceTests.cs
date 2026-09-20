using SensorIngestion.Application.Ingestion;
using SensorIngestion.Infrastructure.JsonLines;

namespace SensorIngestion.IntegrationTests.JsonLines;

public sealed class JsonlFileReadingSourceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenPathIsBlank_ShouldThrowArgumentException(string? path)
    {
        Assert.ThrowsAny<ArgumentException>(() => new JsonlFileReadingSource(path!));
    }

    [Fact]
    public async Task ReadAsync_WhenFileContainsLines_ShouldPreserveContentAndOneBasedLineNumbers()
    {
        await using var file = await TemporaryFile.CreateAsync("first" + Environment.NewLine + Environment.NewLine + "third");
        var source = new JsonlFileReadingSource(file.Path);

        var lines = await ReadAllAsync(source);

        Assert.Collection(lines,
            line => AssertLine(line, 1, "first"),
            line => AssertLine(line, 2, ""),
            line => AssertLine(line, 3, "third"));
    }

    [Fact]
    public async Task ReadAsync_WhenFileIsEmpty_ShouldReturnNoLines()
    {
        await using var file = await TemporaryFile.CreateAsync("");
        var source = new JsonlFileReadingSource(file.Path);

        var lines = await ReadAllAsync(source);

        Assert.Empty(lines);
    }

    [Fact]
    public async Task ReadAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"sensor-ingestion-missing-{Guid.NewGuid():N}.jsonl");
        var source = new JsonlFileReadingSource(path);

        await Assert.ThrowsAsync<FileNotFoundException>(() => ReadAllAsync(source));
    }

    [Fact]
    public async Task ReadAsync_WhenCancellationIsAlreadyRequested_ShouldThrowOperationCanceledException()
    {
        await using var file = await TemporaryFile.CreateAsync("first" + Environment.NewLine + "second");
        var source = new JsonlFileReadingSource(file.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadAllAsync(source, cancellation.Token));
    }

    private static async Task<List<InputLine>> ReadAllAsync(IReadingSource source, CancellationToken cancellationToken = default)
    {
        var lines = new List<InputLine>();
        await foreach (var line in source.ReadAsync(cancellationToken)) lines.Add(line);
        return lines;
    }

    private static void AssertLine(InputLine line, long expectedLineNumber, string expectedContent)
    {
        Assert.Equal(expectedLineNumber, line.LineNumber);
        Assert.Equal(expectedContent, line.Content);
    }

    private sealed class TemporaryFile : IAsyncDisposable
    {
        public string Path { get; }

        private TemporaryFile(string path)
        {
            Path = path;
        }

        public static async Task<TemporaryFile> CreateAsync(string content)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sensor-ingestion-{Guid.NewGuid():N}.jsonl");
            await File.WriteAllTextAsync(path, content);
            return new TemporaryFile(path);
        }

        public ValueTask DisposeAsync()
        {
            if (File.Exists(Path)) File.Delete(Path);
            return ValueTask.CompletedTask;
        }
    }
}
