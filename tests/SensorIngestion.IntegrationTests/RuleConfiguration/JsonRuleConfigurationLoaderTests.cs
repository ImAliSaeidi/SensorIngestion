using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Rules;
using SensorIngestion.Infrastructure.RuleConfiguration;

namespace SensorIngestion.IntegrationTests.RuleConfiguration;

public sealed class JsonRuleConfigurationLoaderTests
{
    [Fact]
    public async Task LoadAsync_WhenConfigurationIsValid_ShouldMapEverySupportedOperator()
    {
        const string json = """
            [
              { "id": "greater-than", "name": "Greater than", "metric": "temperature", "operator": "GreaterThan", "threshold": 80, "enabled": true },
              { "id": "greater-than-or-equal", "name": "Greater than or equal", "metric": "temperature", "operator": "GreaterThanOrEqual", "threshold": 80, "enabled": true },
              { "id": "less-than", "name": "Less than", "metric": "pressure", "operator": "LessThan", "threshold": 2, "enabled": true },
              { "id": "less-than-or-equal", "name": "Less than or equal", "metric": "pressure", "operator": "LessThanOrEqual", "threshold": 2, "enabled": true },
              { "id": "equal", "name": "Equal", "metric": "vibration", "operator": "Equal", "threshold": 0, "enabled": true },
              { "id": "between", "name": "Between", "deviceId": "pump-01", "metric": "pressure", "operator": "Between", "lowerBound": 2, "upperBound": 5, "enabled": false },
              { "id": "sustained", "name": "Sustained", "deviceId": "PUMP-01", "metric": "temperature", "operator": "SustainedAbove", "threshold": 80, "durationSeconds": 30, "enabled": true }
            ]
            """;
        await using var file = await TemporaryRuleFile.CreateAsync(json);
        var loader = new JsonRuleConfigurationLoader(file.Path);

        var rules = await loader.LoadAsync(CancellationToken.None);

        Assert.Equal(7, rules.Count);
        var comparison = rules.Single(x => x.RuleKey == "greater-than");
        Assert.Equal("Greater than", comparison.Name);
        Assert.True(comparison.Enabled);
        Assert.Equal(Metric.Temperature, comparison.Metric);
        Assert.Null(comparison.DeviceId);
        Assert.Equal(RuleOperator.Create("GreaterThan"), comparison.Operator);
        Assert.Equal(80, comparison.Parameters.Single(x => x.Name == "threshold").Value);
        Assert.False(string.IsNullOrWhiteSpace(comparison.ConfigurationHash));

        var between = rules.Single(x => x.RuleKey == "between");
        Assert.False(between.Enabled);
        Assert.Equal("PUMP-01", between.DeviceId);
        Assert.Equal(2, between.Parameters.Single(x => x.Name == "lowerBound").Value);
        Assert.Equal(5, between.Parameters.Single(x => x.Name == "upperBound").Value);

        var sustained = rules.Single(x => x.RuleKey == "sustained");
        Assert.Equal(80, sustained.Parameters.Single(x => x.Name == "threshold").Value);
        Assert.Equal(30, sustained.Parameters.Single(x => x.Name == "durationSeconds").Value);
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public async Task LoadAsync_WhenConfigurationIsInvalid_ShouldThrowInvalidDataException(string json)
    {
        await using var file = await TemporaryRuleFile.CreateAsync(json);
        var loader = new JsonRuleConfigurationLoader(file.Path);

        await Assert.ThrowsAsync<InvalidDataException>(() => loader.LoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        var path = Path.Combine(Path.GetTempPath(), $"missing-rules-{Guid.NewGuid():N}.json");
        var loader = new JsonRuleConfigurationLoader(path);

        await Assert.ThrowsAsync<FileNotFoundException>(() => loader.LoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task LoadAsync_WhenConfigurationFileChanges_ShouldLoadNewValuesWithoutRecreatingLoader()
    {
        const string original = """[{"id":"temperature-limit","name":"Temperature limit","metric":"temperature","operator":"GreaterThan","threshold":80,"enabled":true}]""";
        const string replacement = """[{"id":"temperature-limit","name":"Temperature limit","deviceId":"PUMP-01","metric":"temperature","operator":"GreaterThan","threshold":90,"enabled":false}]""";
        await using var file = await TemporaryRuleFile.CreateAsync(original);
        var loader = new JsonRuleConfigurationLoader(file.Path);

        var originalRule = Assert.Single(await loader.LoadAsync(CancellationToken.None));
        await file.WriteAsync(replacement);
        var replacementRule = Assert.Single(await loader.LoadAsync(CancellationToken.None));

        Assert.Equal(80, originalRule.Parameters.Single(x => x.Name == "threshold").Value);
        Assert.True(originalRule.Enabled);
        Assert.Null(originalRule.DeviceId);
        Assert.Equal(90, replacementRule.Parameters.Single(x => x.Name == "threshold").Value);
        Assert.False(replacementRule.Enabled);
        Assert.Equal("PUMP-01", replacementRule.DeviceId);
        Assert.NotEqual(originalRule.ConfigurationHash, replacementRule.ConfigurationHash);
    }

    [Fact]
    public async Task LoadAsync_WhenOnlyJsonFormattingOrPropertyOrderChanges_ShouldKeepConfigurationHash()
    {
        const string original = """[{"id":"temperature-limit","name":"Temperature limit","metric":"temperature","operator":"GreaterThan","threshold":80,"enabled":true}]""";
        const string reformatted = """
            [
              {
                "enabled": true,
                "threshold": 80,
                "operator": "GreaterThan",
                "metric": "temperature",
                "name": "Temperature limit",
                "id": "temperature-limit"
              }
            ]
            """;
        await using var file = await TemporaryRuleFile.CreateAsync(original);
        var loader = new JsonRuleConfigurationLoader(file.Path);

        var originalRule = Assert.Single(await loader.LoadAsync(CancellationToken.None));
        await file.WriteAsync(reformatted);
        var reformattedRule = Assert.Single(await loader.LoadAsync(CancellationToken.None));

        Assert.Equal(originalRule.ConfigurationHash, reformattedRule.ConfigurationHash);
    }

    [Fact]
    public async Task LoadAsync_WhenCancellationIsRequested_ShouldThrowOperationCanceledException()
    {
        await using var file = await TemporaryRuleFile.CreateAsync("[]");
        var loader = new JsonRuleConfigurationLoader(file.Path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loader.LoadAsync(cancellation.Token));
    }

    public static TheoryData<string> InvalidConfigurations => new()
    {
        "not-json",
        "{}",
        "null",
        """[{"name":"Missing ID","metric":"temperature","operator":"GreaterThan","threshold":80,"enabled":true}]""",
        """[{"id":"rule-1","name":"First","metric":"temperature","operator":"GreaterThan","threshold":80,"enabled":true},{"id":"RULE-1","name":"Second","metric":"temperature","operator":"GreaterThan","threshold":90,"enabled":true}]""",
        """[{"id":"rule-1","name":" ","metric":"temperature","operator":"GreaterThan","threshold":80,"enabled":true}]""",
        """[{"id":"rule-1","name":"Rule","deviceId":" ","metric":"temperature","operator":"GreaterThan","threshold":80,"enabled":true}]""",
        """[{"id":"rule-1","name":"Rule","metric":" ","operator":"GreaterThan","threshold":80,"enabled":true}]""",
        """[{"id":"rule-1","name":"Rule","metric":"temperature","operator":"Unknown","threshold":80,"enabled":true}]""",
        """[{"id":"rule-1","name":"Rule","metric":"temperature","operator":"GreaterThan","enabled":true}]""",
        """[{"id":"rule-1","name":"Rule","metric":"temperature","operator":"Between","lowerBound":5,"upperBound":2,"enabled":true}]""",
        """[{"id":"rule-1","name":"Rule","metric":"temperature","operator":"SustainedAbove","threshold":80,"durationSeconds":0,"enabled":true}]""",
        """[{"id":"rule-1","name":"Rule","metric":"temperature","operator":"GreaterThan","threshold":80}]"""
    };

    private sealed class TemporaryRuleFile : IAsyncDisposable
    {
        public string Path { get; }

        private TemporaryRuleFile(string path)
        {
            Path = path;
        }

        public static async Task<TemporaryRuleFile> CreateAsync(string content)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sensor-rules-{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(path, content);
            return new TemporaryRuleFile(path);
        }

        public Task WriteAsync(string content) => File.WriteAllTextAsync(Path, content);

        public ValueTask DisposeAsync()
        {
            if (File.Exists(Path)) File.Delete(Path);
            return ValueTask.CompletedTask;
        }
    }
}
