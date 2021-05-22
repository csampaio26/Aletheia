namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public class JaccardCS : IRankingStrategy
    {
        public double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed)
        {
            double result = ((double)coveredFailed / ((double)coveredFailed + (double)uncoveredFailed* 1.5 + (double)coveredPassed));

            return result;
        }
    }
}
