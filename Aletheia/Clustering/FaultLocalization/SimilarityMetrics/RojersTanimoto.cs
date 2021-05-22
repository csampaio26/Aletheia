namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public class RojersTanimoto : IRankingStrategy
    {
        public double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed)
        {
            double result = ((double)coveredFailed + (double)uncoveredPassed) /
                ((double)coveredFailed + (double)uncoveredPassed + (double)uncoveredFailed + (double)coveredPassed);

            return result;
        }
    }
}