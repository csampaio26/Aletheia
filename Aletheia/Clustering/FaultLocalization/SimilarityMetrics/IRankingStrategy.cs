namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public interface IRankingStrategy
    {
        double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed);
    }
}
