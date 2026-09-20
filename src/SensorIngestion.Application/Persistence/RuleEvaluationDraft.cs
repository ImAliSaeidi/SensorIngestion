using SensorIngestion.Domain.Readings;
using SensorIngestion.Domain.Rules;

namespace SensorIngestion.Application.Persistence;

public sealed record RuleEvaluationDraft(SensorReading Reading, Rule Rule, bool IsViolated, string? Explanation);
