namespace VibeCoders.Services.Interfaces;

public interface IEvaluationEngine
{
    IReadOnlyList<string> Evaluate(int clientId);

    RankShowcaseSnapshot BuildRankShowcase(int clientId);
}