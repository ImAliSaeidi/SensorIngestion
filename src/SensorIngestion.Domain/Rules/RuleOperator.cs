namespace SensorIngestion.Domain.Rules;

public sealed record RuleOperator
{
    public string Value { get; }

    private RuleOperator(string value)
    {
        Value = value;
    }

    public static RuleOperator Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new RuleOperator(value.Trim());
    }
}
