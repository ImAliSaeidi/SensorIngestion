namespace SensorIngestion.Application.Rules.Evaluation;

public sealed class OperatorEvaluationResult
{
    public bool IsSatisfied { get; }

    public string? Explanation { get; }

    private OperatorEvaluationResult(bool isSatisfied, string? explanation = null)
    {
        IsSatisfied = isSatisfied;

        if (!isSatisfied && string.IsNullOrWhiteSpace(explanation))
            throw new ArgumentNullException(nameof(explanation), "explanation is required for not satisfied operator");

        Explanation = explanation;
    }

    public static OperatorEvaluationResult Satisfied() => new(true);

    public static OperatorEvaluationResult Violated(string explanation) => new(false, explanation);
}
