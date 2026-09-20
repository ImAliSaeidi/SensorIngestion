using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Application.RuleEvaluation;

public sealed class StatelessRuleEvaluatorTests
{
    private static readonly DateTimeOffset Timestamp = new(2025, 6, 1, 8, 33, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_WhenGlobalRuleHasMatchingMetric_ShouldEvaluateRule()
    {
        var strategy = new RecordingOperatorStrategy("Test", isSatisfied: true);
        var evaluator = CreateEvaluator(strategy);
        var reading = CreateReading();
        var rule = CreateRule(deviceId: null);

        var result = evaluator.Evaluate(reading, [rule]);

        Assert.Equal(1, strategy.CallCount);
        Assert.Single(result.Decisions);
    }

    [Fact]
    public void Evaluate_WhenDeviceSpecificRuleMatchesDeviceAndMetric_ShouldEvaluateRule()
    {
        var strategy = new RecordingOperatorStrategy("Test", isSatisfied: true);
        var evaluator = CreateEvaluator(strategy);
        var reading = CreateReading(deviceId: "PUMP-01", metric: Metric.Temperature);
        var rule = CreateRule(deviceId: "PUMP-01", metric: Metric.Temperature);

        var result = evaluator.Evaluate(reading, [rule]);

        Assert.Equal(1, strategy.CallCount);
        Assert.Single(result.Decisions);
    }

    [Theory]
    [InlineData(false, "PUMP-01", "temperature")]
    [InlineData(true, "PUMP-02", "temperature")]
    [InlineData(true, "PUMP-01", "pressure")]
    public void Evaluate_WhenRuleIsNotApplicable_ShouldSkipRule(bool enabled, string deviceId, string metric)
    {
        var strategy = new RecordingOperatorStrategy("Test", isSatisfied: false);
        var evaluator = CreateEvaluator(strategy);
        var reading = CreateReading();
        var rule = CreateRule(enabled: enabled, deviceId: deviceId, metric: Metric.Create(metric));

        var result = evaluator.Evaluate(reading, [rule]);

        Assert.Equal(0, strategy.CallCount);
        Assert.Empty(result.Decisions);
        Assert.Empty(result.Violations);
        Assert.Equal(ReadingClassification.Acceptable, reading.Classification);
    }

    [Fact]
    public void Evaluate_WhenNoRulesApply_ShouldClassifyReadingAsAcceptable()
    {
        var evaluator = CreateEvaluator(new RecordingOperatorStrategy("Test", isSatisfied: false));
        var reading = CreateReading();

        var result = evaluator.Evaluate(reading, []);

        Assert.True(result.IsAcceptable);
        Assert.Empty(result.Decisions);
        Assert.Empty(result.Violations);
        Assert.Equal(ReadingClassification.Acceptable, reading.Classification);
    }

    [Fact]
    public void Evaluate_WhenEveryApplicableRuleIsSatisfied_ShouldClassifyReadingAsAcceptable()
    {
        var first = new RecordingOperatorStrategy("First", isSatisfied: true);
        var second = new RecordingOperatorStrategy("Second", isSatisfied: true);
        var evaluator = CreateEvaluator(first, second);
        var reading = CreateReading();

        var result = evaluator.Evaluate(reading, [CreateRule(ruleKey: "rule-1", operatorName: "First"), CreateRule(ruleKey: "rule-2", operatorName: "Second")]);

        Assert.True(result.IsAcceptable);
        Assert.Equal(2, result.Decisions.Count);
        Assert.Empty(result.Violations);
        Assert.Equal(ReadingClassification.Acceptable, reading.Classification);
    }

    [Fact]
    public void Evaluate_WhenAnApplicableRuleIsViolated_ShouldClassifyReadingAsUnacceptable()
    {
        var evaluator = CreateEvaluator(new RecordingOperatorStrategy("Test", isSatisfied: false, explanation: "Value 75 does not satisfy the rule."));
        var reading = CreateReading(value: 75);

        var result = evaluator.Evaluate(reading, [CreateRule(ruleKey: "minimum-temperature", name: "Minimum temperature")]);

        Assert.False(result.IsAcceptable);
        var violation = Assert.Single(result.Violations);
        Assert.Equal("minimum-temperature", violation.RuleKey);
        Assert.Equal("Minimum temperature", violation.RuleName);
        Assert.Equal(RuleOperator.Create("Test"), violation.Operator);
        Assert.Equal("Value 75 does not satisfy the rule.", violation.Explanation);
        Assert.Equal(ReadingClassification.Unacceptable, reading.Classification);
    }

    [Fact]
    public void Evaluate_WhenSeveralRulesAreViolated_ShouldCollectEveryViolationWithoutShortCircuiting()
    {
        var first = new RecordingOperatorStrategy("First", isSatisfied: false, explanation: "First violation.");
        var second = new RecordingOperatorStrategy("Second", isSatisfied: false, explanation: "Second violation.");
        var evaluator = CreateEvaluator(first, second);
        var reading = CreateReading();
        var rules = new[]
        {
            CreateRule(ruleKey: "rule-1", operatorName: "First"),
            CreateRule(ruleKey: "rule-2", operatorName: "Second")
        };

        var result = evaluator.Evaluate(reading, rules);

        Assert.Equal(1, first.CallCount);
        Assert.Equal(1, second.CallCount);
        Assert.Equal(2, result.Decisions.Count);
        Assert.Collection(result.Violations,
            violation => Assert.Equal("rule-1", violation.RuleKey),
            violation => Assert.Equal("rule-2", violation.RuleKey));
        Assert.Equal(ReadingClassification.Unacceptable, reading.Classification);
    }

    [Fact]
    public void Evaluate_ShouldPassReadingValueAndRuleParametersToStrategy()
    {
        var strategy = new RecordingOperatorStrategy("Test", isSatisfied: true);
        var evaluator = CreateEvaluator(strategy);
        var reading = CreateReading(value: 42.5);
        var parameters = new[] { RuleParameter.Create(RuleParameterNames.Threshold, 40) };

        evaluator.Evaluate(reading, [CreateRule(parameters: parameters)]);

        Assert.Equal(42.5, strategy.LastValue);
        Assert.Same(parameters[0], Assert.Single(strategy.LastParameters!));
    }

    private static StatelessRuleEvaluator CreateEvaluator(params IRuleOperatorStrategy[] strategies)
    {
        return new StatelessRuleEvaluator(new RuleOperatorRegistry(strategies));
    }

    private static SensorReading CreateReading(string deviceId = "PUMP-01", Metric? metric = null, double value = 80)
    {
        return new SensorReading(deviceId, metric ?? Metric.Temperature, Timestamp, value, sequence: 1);
    }

    private static Rule CreateRule(string ruleKey = "rule-1", string name = "Test rule", bool enabled = true, Metric? metric = null, string? deviceId = null, string operatorName = "Test", IReadOnlyCollection<RuleParameter>? parameters = null)
    {
        return new Rule(ruleKey, 1, name, enabled, metric ?? Metric.Temperature, deviceId, RuleOperator.Create(operatorName), parameters ?? [RuleParameter.Create(RuleParameterNames.Threshold, 80)], $"hash-{ruleKey}", Timestamp);
    }

    private sealed class RecordingOperatorStrategy : IRuleOperatorStrategy
    {
        private readonly bool _isSatisfied;
        private readonly string _explanation;

        public RuleOperator Operator { get; }
        public int CallCount { get; private set; }
        public double? LastValue { get; private set; }
        public IReadOnlyCollection<RuleParameter>? LastParameters { get; private set; }

        public RecordingOperatorStrategy(string operatorName, bool isSatisfied, string explanation = "Rule was violated.")
        {
            Operator = RuleOperator.Create(operatorName);
            _isSatisfied = isSatisfied;
            _explanation = explanation;
        }

        public OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters)
        {
            CallCount++;
            LastValue = value;
            LastParameters = parameters;
            return _isSatisfied ? OperatorEvaluationResult.Satisfied() : OperatorEvaluationResult.Violated(_explanation);
        }
    }
}
