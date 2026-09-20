using System.Text.Json.Serialization;

namespace SensorIngestion.Infrastructure.RuleConfiguration.Models;

internal sealed class RuleJsonDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("metric")]
    public string? Metric { get; init; }

    [JsonPropertyName("operator")]
    public string? Operator { get; init; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    [JsonPropertyName("threshold")]
    public double? Threshold { get; init; }

    [JsonPropertyName("lowerBound")]
    public double? LowerBound { get; init; }

    [JsonPropertyName("upperBound")]
    public double? UpperBound { get; init; }

    [JsonPropertyName("durationSeconds")]
    public double? DurationSeconds { get; init; }
}