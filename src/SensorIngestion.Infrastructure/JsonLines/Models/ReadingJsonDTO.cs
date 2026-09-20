using System.Globalization;
using System.Text.Json.Serialization;

namespace SensorIngestion.Infrastructure.JsonLines.Models;

internal sealed class ReadingJsonDto
{
    private static readonly string[] TimestampFormats =
        [
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
        ];

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("metric")]
    public string? Metric { get; init; }

    [JsonPropertyName("ts")]
    public string? Timestamp { get; init; }

    [JsonPropertyName("value")]
    public double? Value { get; init; }

    [JsonPropertyName("seq")]
    public long? Sequence { get; init; }

    public DateTimeOffset ParsedTimestamp { get; private set; }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(DeviceId))
            return "deviceId is required.";

        if (string.IsNullOrWhiteSpace(Metric))
            return "metric is required.";

        if (string.IsNullOrWhiteSpace(Timestamp))
            return "ts is required.";

        if (Value is null || !double.IsFinite(Value.Value))
            return "value must be a finite number.";

        if (Sequence is null || Sequence < 0)
            return "seq must be a non-negative integer.";

        if (!TryParseTimestamp(Timestamp, out var timestamp))
            return "ts must be a valid ISO-8601 timestamp.";

        ParsedTimestamp = timestamp;

        return null;
    }

    private static bool TryParseTimestamp(string value, out DateTimeOffset timestamp)
    {
        return DateTimeOffset.TryParseExact(
            value,
            TimestampFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out timestamp);
    }
}