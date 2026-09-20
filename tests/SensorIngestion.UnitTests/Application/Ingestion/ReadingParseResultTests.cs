using SensorIngestion.Application.Ingestion;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;

namespace SensorIngestion.UnitTests.Application.Ingestion;

public sealed class ReadingParseResultTests
{
    [Fact]
    public void Success_WhenReadingIsValid_ShouldCreateSuccessfulResult()
    {
        var reading = CreateReading();

        var result = ReadingParseResult.Success(reading);

        Assert.Same(reading, result.Reading);
        Assert.Null(result.Rejection);
        Assert.True(result.IsSuccess);
        Assert.True(result.WasParsed);
    }

    [Fact]
    public void Success_WhenReadingIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ReadingParseResult.Success(null!));
    }

    [Theory]
    [InlineData(RejectionCategory.EmptyLine, false)]
    [InlineData(RejectionCategory.MalformedInput, false)]
    [InlineData(RejectionCategory.InvalidRoot, false)]
    [InlineData(RejectionCategory.MissingRequiredField, true)]
    [InlineData(RejectionCategory.InvalidFieldType, true)]
    [InlineData(RejectionCategory.InvalidFieldValue, true)]
    [InlineData(RejectionCategory.InvalidTimestamp, true)]
    public void Failure_WhenRejectionIsValid_ShouldCreateFailedResult(RejectionCategory category, bool expectedWasParsed)
    {
        var rejection = new ReadingRejection(1, category, "Rejected");

        var result = ReadingParseResult.Failure(rejection);

        Assert.Null(result.Reading);
        Assert.Same(rejection, result.Rejection);
        Assert.False(result.IsSuccess);
        Assert.Equal(expectedWasParsed, result.WasParsed);
    }

    [Fact]
    public void Failure_WhenRejectionIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ReadingParseResult.Failure(null!));
    }

    private static SensorReading CreateReading() => new("PUMP-01", Metric.Temperature, DateTimeOffset.UtcNow, 42.5, 1);
}
