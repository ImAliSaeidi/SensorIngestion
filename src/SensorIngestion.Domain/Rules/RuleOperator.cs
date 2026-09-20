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
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value), "rule operator cannot be null");

        return new RuleOperator(value.Trim());
    }
}
