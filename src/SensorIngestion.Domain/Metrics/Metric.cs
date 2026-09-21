namespace SensorIngestion.Domain.Metrics;

public sealed record Metric
{
    public static Metric Temperature { get; } = new Metric("temperature");

    public static Metric Pressure { get; } = new Metric("pressure");

    public static Metric Vibration { get; } = new Metric("vibration");

    public string Value { get; }

    private Metric(string value)
    {
        Value = value;
    }

    public static Metric Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new Metric(value.Trim().ToLowerInvariant());
    }
}
