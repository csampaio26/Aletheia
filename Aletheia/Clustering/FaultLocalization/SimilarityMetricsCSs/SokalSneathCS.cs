﻿namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public class SokalSneathCS : IRankingStrategy
    {
        public double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed)
        {
            double result = (2 * ((double)coveredFailed + (double)uncoveredPassed)) /
                ((2 * ((double)coveredFailed + (double)uncoveredPassed)) + (double)uncoveredFailed * 0.5 + (double)coveredPassed);

            return result;
        }
    }
}
