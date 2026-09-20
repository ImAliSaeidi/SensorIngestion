using SensorIngestion.Application.Ingestion;

namespace SensorIngestion.UnitTests.Application.Ingestion;

public sealed class InputLineTests
{
    private const string ValidContent = """{"deviceId":"PUMP-01"}""";

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void Constructor_WhenLineNumberIsNotPositive_ShouldThrowArgumentOutOfRangeException(long lineNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputLine(lineNumber, ValidContent));
    }

    [Fact]
    public void Constructor_WhenArgumentsAreValid_ShouldPersistValues()
    {
        const long lineNumber = 12;

        var inputLine = new InputLine(lineNumber, ValidContent);

        Assert.Equal(lineNumber, inputLine.LineNumber);
        Assert.Equal(ValidContent, inputLine.Content);
    }

    [Fact]
    public void Constructor_WhenContentIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new InputLine(1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_WhenContentIsEmptyOrWhiteSpace_ShouldPersistContent(string content)
    {
        var inputLine = new InputLine(1, content);
        Assert.Equal(content, inputLine.Content);
    }
}
