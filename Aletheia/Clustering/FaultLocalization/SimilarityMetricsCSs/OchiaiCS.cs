using System;

namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public class OchiaiCS : IRankingStrategy
    {
        public double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed)
        {
            double totalFailed = coveredFailed + uncoveredFailed * 1.5;
            double totalCovered = coveredFailed + coveredPassed;
            double numerator = (double)coveredFailed;
            double denominator = Math.Sqrt(totalFailed * totalCovered);
            double result = numerator/denominator;

            return result;
        }
    }
}
