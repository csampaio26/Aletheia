using System;

namespace Aletheia.Clustering.FaultLocalization.SimilarityMetrics
{
    public class DstarCS : IRankingStrategy
    {
        private int star = 1;
        public DstarCS()
        {

        }
        public DstarCS(int s)
        {
            star = s;
        }
        public double calculateSuspiciousness(int coveredFailed, int uncoveredFailed, int coveredPassed, int uncoveredPassed)
        {
            if (coveredPassed + uncoveredFailed <= 0) return Int32.MaxValue;
            double result = (Math.Pow((double)coveredFailed, star) / ((double)uncoveredFailed * 0.5 + (double)coveredPassed));

            return result;
        }
    }
}
