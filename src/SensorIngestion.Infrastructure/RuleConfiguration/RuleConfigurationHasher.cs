using SensorIngestion.Domain.Rules;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SensorIngestion.Infrastructure.RuleConfiguration;

internal static class RuleConfigurationHasher
{
    public static string Compute(string ruleKey, string name, bool enabled, string metric, string? deviceId, string @operator, IEnumerable<RuleParameter> parameters)
    {
        var parameterText = string.Join(
            "|",
            parameters
                .OrderBy(parameter => parameter.Name, StringComparer.Ordinal)
                .Select(parameter => $"{parameter.Name}:{parameter.Value.ToString("R", CultureInfo.InvariantCulture)}"));

        var canonicalValue = string.Join(
            "\n",
            ruleKey.Trim(),
            name.Trim(),
            enabled.ToString(),
            metric.Trim().ToLowerInvariant(),
            deviceId?.Trim() ?? string.Empty,
            @operator.Trim(),
            parameterText);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalValue));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
