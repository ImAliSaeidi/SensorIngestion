using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Domain.Rules;

public sealed class RuleTests
{
    [Fact]
    public void Constructor_WhenArgumentsAreValid_ShouldCreateNormalizedRule()
    {
        var createdAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.FromHours(3.5));
        var parameter = RuleParameter.Create("threshold", 80);

        var rule = CreateRule(
            ruleKey: "  high-temperature  ",
            name: "  High temperature  ",
            deviceId: "  pump-01  ",
            parameters: [parameter],
            configurationHash: "  abc123  ",
            createdAt: createdAt);

        Assert.Equal("high-temperature", rule.RuleKey);
        Assert.Equal(1, rule.Version);
        Assert.Equal("High temperature", rule.Name);
        Assert.True(rule.Enabled);
        Assert.Equal(Metric.Temperature, rule.Metric);
        Assert.Equal("PUMP-01", rule.DeviceId);
        Assert.Equal("GreaterThan", rule.Operator.Value);
        Assert.Equal("abc123", rule.ConfigurationHash);
        Assert.Equal(TimeSpan.Zero, rule.CreatedAt.Offset);
        Assert.Equal(createdAt.UtcDateTime, rule.CreatedAt.UtcDateTime);
        Assert.Equal(parameter, Assert.Single(rule.Parameters));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenRuleKeyIsBlank_ShouldThrowArgumentException(string? ruleKey)
    {
        Assert.Throws<ArgumentException>(() => CreateRule(ruleKey: ruleKey!));
    }

    [Fact]
    public void Constructor_WhenRuleKeyIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CreateRule(ruleKey: null!));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WhenVersionIsNotPositive_ShouldThrowArgumentOutOfRangeException(int version)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateRule(version: version));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenNameIsBlank_ShouldThrowArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => CreateRule(name: name!));
    }

    [Fact]
    public void Constructor_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CreateRule(name: null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WhenConfigurationHashIsBlank_ShouldThrowArgumentException(string? hash)
    {
        Assert.Throws<ArgumentException>(() => CreateRule(configurationHash: hash!));
    }

    [Fact]
    public void Constructor_WhenConfigurationHashIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CreateRule(configurationHash: null!));
    }

    [Fact]
    public void Constructor_WhenMetricIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Rule(
            "high-temperature",
            1,
            "High temperature",
            true,
            null!,
            null,
            RuleOperator.Create("GreaterThan"),
            [RuleParameter.Create("threshold", 80)],
            "abc123",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_WhenOperatorIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Rule(
            "high-temperature",
            1,
            "High temperature",
            true,
            Metric.Temperature,
            null,
            null!,
            [RuleParameter.Create("threshold", 80)],
            "abc123",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_WhenParametersAreNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new Rule(
            "high-temperature",
            1,
            "High temperature",
            true,
            Metric.Temperature,
            null,
            RuleOperator.Create("GreaterThan"),
            null!,
            "abc123",
            DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_WhenParameterNamesAreDuplicatedIgnoringCase_ShouldThrowArgumentException()
    {
        var parameters = new[] { RuleParameter.Create("threshold", 80), RuleParameter.Create("THRESHOLD", 90) };

        Assert.Throws<ArgumentException>(() => CreateRule(parameters: parameters));
    }

    [Fact]
    public void Constructor_WhenSourceParameterListChanges_ShouldKeepOriginalParameters()
    {
        var parameters = new List<RuleParameter> { RuleParameter.Create("threshold", 80) };
        var rule = CreateRule(parameters: parameters);

        parameters.Add(RuleParameter.Create("durationSeconds", 30));

        Assert.Single(rule.Parameters);
    }

    [Fact]
    public void Parameters_WhenCastToCollection_ShouldNotAllowMutation()
    {
        var rule = CreateRule();
        var collection = Assert.IsType<ICollection<RuleParameter>>(rule.Parameters, exactMatch: false);

        Assert.Throws<NotSupportedException>(() => collection.Add(RuleParameter.Create("minimum", 1)));
    }

    [Fact]
    public void AppliesTo_WhenRuleIsDisabled_ShouldReturnFalse()
    {
        var rule = CreateRule(enabled: false, deviceId: null);

        Assert.False(rule.AppliesTo(CreateReading()));
    }

    [Fact]
    public void AppliesTo_WhenMetricDiffers_ShouldReturnFalse()
    {
        var rule = CreateRule(deviceId: null);

        Assert.False(rule.AppliesTo(CreateReading(metric: Metric.Pressure)));
    }

    [Fact]
    public void AppliesTo_WhenDeviceIsNotSpecifiedAndMetricMatches_ShouldReturnTrue()
    {
        var rule = CreateRule(deviceId: null);

        Assert.True(rule.AppliesTo(CreateReading()));
    }

    [Fact]
    public void AppliesTo_WhenDeviceMatchesIgnoringCase_ShouldReturnTrue()
    {
        var rule = CreateRule(deviceId: "pump-01");

        Assert.True(rule.AppliesTo(CreateReading(deviceId: "PUMP-01")));
    }

    [Fact]
    public void AppliesTo_WhenDeviceDiffers_ShouldReturnFalse()
    {
        var rule = CreateRule(deviceId: "PUMP-02");

        Assert.False(rule.AppliesTo(CreateReading(deviceId: "PUMP-01")));
    }

    private static Rule CreateRule(
        string ruleKey = "high-temperature",
        int version = 1,
        string name = "High temperature",
        bool enabled = true,
        Metric? metric = null,
        string? deviceId = "PUMP-01",
        RuleOperator? @operator = null,
        IEnumerable<RuleParameter>? parameters = null,
        string configurationHash = "abc123",
        DateTimeOffset? createdAt = null)
        => new(
            ruleKey,
            version,
            name,
            enabled,
            metric ?? Metric.Temperature,
            deviceId,
            @operator ?? RuleOperator.Create("GreaterThan"),
            parameters ?? [RuleParameter.Create("threshold", 80)],
            configurationHash,
            createdAt ?? DateTimeOffset.UtcNow);

    private static SensorReading CreateReading(string deviceId = "PUMP-01", Metric? metric = null) => new(deviceId, metric ?? Metric.Temperature, DateTimeOffset.UtcNow, 75, 1);
}
