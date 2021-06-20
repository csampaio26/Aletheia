namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public class RojersTanimotoCS : IRankingStrategy
    {
        public double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed)
        {
            double result = ((double)coveredFailed + (double)uncoveredPassed) /
                ((double)coveredFailed + (double)uncoveredPassed + (double)uncoveredFailed * 0.5 + (double)coveredPassed);

            return result;
        }
    }
}