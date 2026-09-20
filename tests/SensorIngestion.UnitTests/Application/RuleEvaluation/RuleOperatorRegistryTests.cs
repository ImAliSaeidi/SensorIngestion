using SensorIngestion.Application.Rules;
using SensorIngestion.Application.Rules.Evaluation;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.UnitTests.Application.RuleEvaluation;

public sealed class RuleOperatorRegistryTests
{
    [Fact]
    public void Resolve_WhenOperatorIsRegistered_ShouldReturnMatchingStrategy()
    {
        var expected = new StubOperatorStrategy(RuleOperatorNames.GreaterThan, isSatisfied: true);
        var registry = new RuleOperatorRegistry([expected]);

        var result = registry.Resolve(RuleOperator.Create(RuleOperatorNames.GreaterThan));

        Assert.Same(expected, result);
    }

    [Fact]
    public void Constructor_WhenOperatorNameIsRegisteredMoreThanOnce_ShouldThrowInvalidOperationException()
    {
        var strategies = new IRuleOperatorStrategy[]
        {
            new StubOperatorStrategy(RuleOperatorNames.GreaterThan, isSatisfied: true),
            new StubOperatorStrategy(RuleOperatorNames.GreaterThan, isSatisfied: false)
        };

        var exception = Assert.Throws<InvalidOperationException>(() => new RuleOperatorRegistry(strategies));

        Assert.Contains(RuleOperatorNames.GreaterThan, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_WhenOperatorIsNotRegistered_ShouldThrowNotSupportedException()
    {
        var registry = new RuleOperatorRegistry([]);

        var exception = Assert.Throws<NotSupportedException>(() => registry.Resolve(RuleOperator.Create("CustomOperator")));

        Assert.Contains("CustomOperator", exception.Message, StringComparison.Ordinal);
    }

    private sealed class StubOperatorStrategy : IRuleOperatorStrategy
    {
        private readonly bool _isSatisfied;

        public RuleOperator Operator { get; }

        public StubOperatorStrategy(string operatorName, bool isSatisfied)
        {
            Operator = RuleOperator.Create(operatorName);
            _isSatisfied = isSatisfied;
        }

        public OperatorEvaluationResult Evaluate(double value, IReadOnlyCollection<RuleParameter> parameters)
        {
            return _isSatisfied ? OperatorEvaluationResult.Satisfied() : OperatorEvaluationResult.Violated("Stub violation.");
        }
    }
}
