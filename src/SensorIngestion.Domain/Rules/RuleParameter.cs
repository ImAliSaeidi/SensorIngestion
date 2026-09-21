namespace SensorIngestion.Domain.Rules;

public sealed record RuleParameter
{
    public string Name { get; }

    public double Value { get; }

    private RuleParameter(string name, double value)
    {
        Name = name.Trim();
        Value = value;
    }

    public static RuleParameter Create(string name, double value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!double.IsFinite(value))
            throw new ArgumentException("rule parameter value must be finite", nameof(value));

        return new RuleParameter(name, value);
    }
}