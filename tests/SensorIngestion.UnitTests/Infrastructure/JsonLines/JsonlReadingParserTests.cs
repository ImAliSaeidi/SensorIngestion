using SensorIngestion.Application.Ingestion;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Infrastructure.JsonLines;

namespace SensorIngestion.UnitTests.Infrastructure.JsonLines;

public sealed class JsonlReadingParserTests
{
    private const long LineNumber = 17;
    private readonly JsonlReadingParser _parser = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("null")]
    public void Parse_WhenLineCannotRepresentAReading_ShouldReturnRejection(string content)
    {
        var result = Parse(content);

        AssertRejected(result);
    }

    [Theory]
    [InlineData("{\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21}")]
    [InlineData("{\"deviceId\":null,\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":12,\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"   \",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":null,\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"   \",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":null,\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"not-a-timestamp\",\"value\":67.21,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":null,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":\"67.21\",\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":1e400,\"seq\":1199}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":null}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":1.5}")]
    [InlineData("{\"deviceId\":\"PUMP-01\",\"metric\":\"temperature\",\"ts\":\"2025-06-01T08:33:00Z\",\"value\":67.21,\"seq\":-1}")]
    public void Parse_WhenReadingIsSemanticallyInvalid_ShouldReturnRejection(string content)
    {
        var result = Parse(content);

        AssertRejected(result);
    }

    [Theory]
    [InlineData("2025-06-01T08:33:00")]
    [InlineData("2025-06-01T08:33:00+03:30")]
    public void Parse_WhenTimestampIsNotUtcWithZDesignator_ShouldReturnRejection(string timestamp)
    {
        var content = $$"""{"deviceId":"PUMP-01","metric":"temperature","ts":"{{timestamp}}","value":67.21,"seq":1199}""";

        var result = Parse(content);

        AssertRejected(result);
        Assert.True(result.WasParsed);
    }

    [Fact]
    public void Parse_WhenJsonObjectHasInvalidFieldType_ShouldReturnParsedRejection()
    {
        const string content = """{"deviceId":"PUMP-01","metric":"temperature","ts":"2025-06-01T08:33:00Z","value":"67.21","seq":1199}""";

        var result = Parse(content);

        AssertRejected(result);
        Assert.True(result.WasParsed);
    }

    [Fact]
    public void Parse_WhenReadingIsValid_ShouldReturnPendingSensorReading()
    {
        const string content = """{"deviceId":"PUMP-01","metric":"temperature","ts":"2025-06-01T08:33:00Z","value":67.21,"seq":1199}""";

        var result = Parse(content);

        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.True(result.WasParsed);
        Assert.Null(result.Rejection);
        var reading = Assert.IsType<SensorReading>(result.Reading);
        Assert.Equal("PUMP-01", reading.DeviceId);
        Assert.Equal(Metric.Temperature, reading.Metric);
        Assert.Equal(new DateTimeOffset(2025, 6, 1, 8, 33, 0, TimeSpan.Zero), reading.Timestamp);
        Assert.Equal(67.21, reading.Value);
        Assert.Equal(1199, reading.Sequence);
        Assert.Equal(ReadingClassification.Pending, reading.Classification);
    }

    private ReadingParseResult Parse(string content) => _parser.Parse(new InputLine(LineNumber, content));

    private static void AssertRejected(ReadingParseResult result)
    {
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Reading);
        var rejection = Assert.IsType<ReadingRejection>(result.Rejection);
        Assert.Equal(LineNumber, rejection.LineNumber);
        Assert.False(string.IsNullOrWhiteSpace(rejection.Reason));
    }
}
