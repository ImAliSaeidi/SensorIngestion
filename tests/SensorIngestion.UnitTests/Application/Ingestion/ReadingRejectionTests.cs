using SensorIngestion.Application.Ingestion;

namespace SensorIngestion.UnitTests.Application.Ingestion;

public sealed class ReadingRejectionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void Constructor_WhenLineNumberIsNotPositive_ShouldThrowArgumentOutOfRangeException(long lineNumber)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ReadingRejection(lineNumber, RejectionCategory.EmptyLine, "rejected"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenReasonIsBlank_ShouldThrowArgumentException(string? reason)
    {
        Assert.Throws<ArgumentException>(() => new ReadingRejection(1, RejectionCategory.EmptyLine, reason!));
    }

    [Fact]
    public void Constructor_WhenReasonContainsOuterWhitespace_ShouldTrimReason()
    {
        var rejection = new ReadingRejection(1, RejectionCategory.EmptyLine, "  rejected  ");

        Assert.Equal("rejected", rejection.Reason);
    }

    [Fact]
    public void Constructor_WhenFieldNameContainsOuterWhitespace_ShouldTrimFieldName()
    {
        var rejection = new ReadingRejection(1, RejectionCategory.InvalidFieldValue, "rejected", "  value  ");

        Assert.Equal("value", rejection.FieldName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenFieldNameIsBlank_ShouldNormalizeFieldNameToNull(string? fieldName)
    {
        var rejection = new ReadingRejection(1, RejectionCategory.InvalidFieldValue, "rejected", fieldName);

        Assert.Null(rejection.FieldName);
    }

    [Fact]
    public void Constructor_WhenArgumentsAreValid_ShouldPersistValues()
    {
        const long lineNumber = 12;
        const RejectionCategory category = RejectionCategory.InvalidTimestamp;

        var rejection = new ReadingRejection(lineNumber, category, "invalid timestamp", "ts");

        Assert.Equal(lineNumber, rejection.LineNumber);
        Assert.Equal(category, rejection.Category);
        Assert.Equal("invalid timestamp", rejection.Reason);
        Assert.Equal("ts", rejection.FieldName);
    }
}
