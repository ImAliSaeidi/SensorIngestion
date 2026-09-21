using SensorIngestion.Application.Abstractions.Rules.Evaluation;
using SensorIngestion.Application.Abstractions.Rules.Evaluation.Stateful;
using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Metrics;
using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Application.RuleEvaluation;

public sealed class RuleEngineTests
{
    private static readonly DateTimeOffset Timestamp = new(2025, 6, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_WhenStatelessRuleContainsDurationParameter_ShouldStillUseRegisteredStatelessStrategy()
    {
        var strategy = new RecordingStatelessStrategy("CustomComparison", isSatisfied: true);
        var engine = CreateEngine([strategy], []);
        var reading = CreateReading();
        var rule = CreateRule("CustomComparison", [RuleParameter.Create(RuleParameterNames.DurationSeconds, 30)]);

        var result = engine.Evaluate([reading], [rule]);

        Assert.Equal(1, strategy.CallCount);
        Assert.Single(result.Decisions);
        Assert.Empty(result.Episodes);
        Assert.Equal(ReadingClassification.Acceptable, reading.Classification);
    }

    [Fact]
    public void Evaluate_WhenStatefulEvaluatorIsRegisteredWithoutDurationParameter_ShouldDispatchByOperatorCapability()
    {
        var evaluator = new RecordingStatefulEvaluator("CountWithinWindow", isViolated: true);
        var engine = CreateEngine([], [evaluator]);
        var reading = CreateReading();
        var rule = CreateRule("CountWithinWindow", [RuleParameter.Create(RuleParameterNames.Threshold, 3)]);

        var result = engine.Evaluate([reading], [rule]);

        Assert.Equal(1, evaluator.CallCount);
        Assert.Single(result.Decisions);
        Assert.Equal(ReadingClassification.Unacceptable, reading.Classification);
    }

    [Fact]
    public void Evaluate_ShouldClassifyEachReadingOnceFromAllDecisions()
    {
        var strategy = new ValueAwareStatelessStrategy();
        var statefulEvaluator = new RecordingStatefulEvaluator("StatefulPass", isViolated: false);
        var engine = CreateEngine([strategy], [statefulEvaluator]);
        var violatedReading = CreateReading(value: 40, sequence: 1);
        var acceptedReading = CreateReading(value: 60, sequence: 2);
        var statelessRule = CreateRule(strategy.Operator.Value, [RuleParameter.Create(RuleParameterNames.Threshold, 50)]);
        var statefulRule = CreateRule(statefulEvaluator.Operator.Value, [RuleParameter.Create(RuleParameterNames.Threshold, 1)]);

        engine.Evaluate([violatedReading, acceptedReading], [statelessRule, statefulRule]);

        Assert.Equal(ReadingClassification.Unacceptable, violatedReading.Classification);
        Assert.Equal(ReadingClassification.Acceptable, acceptedReading.Classification);
    }

    private static RuleEngine CreateEngine(IReadOnlyCollection<IRuleOperatorStrategy> statelessStrategies, IReadOnlyCollection<IStatefulRuleEvaluator> statefulEvaluators)
        => new(new StatelessRuleEvaluator(new RuleOperatorRegistry(statelessStrategies)), new StatefulRuleEvaluatorRegistry(statefulEvaluators));

    private static SensorReading CreateReading(double value = 80, long sequence = 1)
        => new("PUMP-01", Metric.Temperature, Timestamp.AddSeconds(sequence), value, sequence);

    private static Rule CreateRule(string operatorName, IReadOnlyCollection<RuleParameter> parameters)
        => new($"rule-{operatorName}", 1, operatorName, true, Metric.Temperature, null, RuleOperator.Create(operatorName), parameters, $"hash-{operatorName}", Timestamp);

    private sealed class RecordingStatelessStrategy(string operatorName, bool isSatisfied) : IRuleOperatorStrategy
    {
        public RuleOperator Operator { get; } = RuleOperator.Create(operatorName);

        public int CallCount { get; private set; }

        public OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters)
        {
            CallCount++;
            return isSatisfied ? OperatorEvaluationResult.Satisfied() : OperatorEvaluationResult.Violated("Rule was violated.");
        }
    }

    private sealed class ValueAwareStatelessStrategy : IRuleOperatorStrategy
    {
        public RuleOperator Operator { get; } = RuleOperator.Create("ValueAware");

        public OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters)
        {
            var threshold = parameters.Single(x => x.Name == RuleParameterNames.Threshold).Value;
            return value >= threshold ? OperatorEvaluationResult.Satisfied() : OperatorEvaluationResult.Violated("Value is below threshold.");
        }
    }

    private sealed class RecordingStatefulEvaluator(string operatorName, bool isViolated) : IStatefulRuleEvaluator
    {
        public RuleOperator Operator { get; } = RuleOperator.Create(operatorName);

        public int CallCount { get; private set; }

        public StatefulRuleEvaluationResult Evaluate(IReadOnlyCollection<ReadingStream> streams, Rule rule)
        {
            CallCount++;
            var decisions = streams
                .SelectMany(stream => stream.Readings)
                .Select(reading => new ReadingRuleEvaluationDecision(reading, new RuleEvaluationDecision(rule, isViolated, isViolated ? "Stateful violation." : null)))
                .ToArray();
            return new StatefulRuleEvaluationResult(decisions, []);
        }
    }
}
