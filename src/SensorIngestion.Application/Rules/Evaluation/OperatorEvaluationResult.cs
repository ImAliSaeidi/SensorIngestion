namespace SensorIngestion.Application.Rules.Evaluation;

public sealed class OperatorEvaluationResult
{
    public bool IsSatisfied { get; }

    public string? Explanation { get; }

    private OperatorEvaluationResult(bool isSatisfied, string? explanation = null)
    {
        IsSatisfied = isSatisfied;

        if (!isSatisfied)
            ArgumentException.ThrowIfNullOrWhiteSpace(explanation);

        Explanation = explanation;
    }

    public static OperatorEvaluationResult Satisfied() => new(true);

    public static OperatorEvaluationResult Violated(string explanation) => new(false, explanation);
}
