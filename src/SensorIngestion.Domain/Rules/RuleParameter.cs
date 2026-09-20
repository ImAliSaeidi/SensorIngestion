namespace SensorIngestion.Domain.Rules;

public sealed record RuleParameter
{
    public string Name { get; }

    public double Value { get; }

    private RuleParameter(string name, double value)
    {
        Name = name;
        Value = value;
    }

    public static RuleParameter Create(string name, double value)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name), "rule parameter name cannot be null;");

        if (!double.IsFinite(value))
            throw new ArgumentException("rule parameter value must be finite", nameof(value));

        return new RuleParameter(name, value);
    }
}