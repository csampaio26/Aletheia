namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public class Jaccard : IRankingStrategy
    {
        public double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed)
        {
            double result = ((double)coveredFailed / ((double)coveredFailed + (double)uncoveredFailed + (double)coveredPassed));

            return result;
        }
    }
}
