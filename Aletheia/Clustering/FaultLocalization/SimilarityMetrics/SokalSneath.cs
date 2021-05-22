namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public class SokalSneath : IRankingStrategy
    {
        public double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed)
        {
            double result = (2 * ((double)coveredFailed + (double)uncoveredPassed)) /
                ((2 * ((double)coveredFailed + (double)uncoveredPassed)) + (double)uncoveredFailed + (double)coveredPassed);

            return result;
        }
    }
}
