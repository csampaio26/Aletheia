﻿namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public class Tarantula: IRankingStrategy
    {
        public double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed)
        {
            double totalFailed = coveredFailed + uncoveredFailed;
            double totalPassed = coveredPassed + uncoveredPassed;
            double failRatio = (double)coveredFailed / totalFailed;
            double passRatio = (double)coveredPassed / totalPassed;
            double result = failRatio / (failRatio + passRatio);

            return result;
        }
    }
}
