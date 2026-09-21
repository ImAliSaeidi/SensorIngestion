using SensorIngestion.Application.Abstractions.Rules.Evaluation.Stateful;
using SensorIngestion.Application.Ingestion.Preprocessing;
using SensorIngestion.Application.Rules.Evaluation.Stateful;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Application.RuleEvaluation.Stateful;

public sealed class StatefulRuleEvaluatorRegistryTests
{
    [Fact]
    public void TryResolve_WhenEvaluatorIsRegistered_ShouldReturnIt()
    {
        var expected = new StubStatefulEvaluator("Windowed");
        var registry = new StatefulRuleEvaluatorRegistry([expected]);

        var found = registry.TryResolve(RuleOperator.Create("Windowed"), out var actual);

        Assert.True(found);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void Constructor_WhenOperatorIsRegisteredMoreThanOnce_ShouldThrow()
    {
        var evaluators = new IStatefulRuleEvaluator[] { new StubStatefulEvaluator("Windowed"), new StubStatefulEvaluator("Windowed") };

        Assert.Throws<InvalidOperationException>(() => new StatefulRuleEvaluatorRegistry(evaluators));
    }

    private sealed class StubStatefulEvaluator(string operatorName) : IStatefulRuleEvaluator
    {
        public RuleOperator Operator { get; } = RuleOperator.Create(operatorName);

        public StatefulRuleEvaluationResult Evaluate(IReadOnlyCollection<ReadingStream> streams, Rule rule)
            => new([], []);
    }
}
