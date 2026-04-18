namespace VibeCoders.Services;

public interface IEvaluationEngine
{
    IReadOnlyList<string> Evaluate(int clientId);
}
