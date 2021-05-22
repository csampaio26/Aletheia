using Aletheia.Clustering.FaultLocalization.SimilarityMetrics;
using System.Collections.Generic;

namespace Aletheia.Clustering.FaultLocalization
{
    public class FaultLocalizer
    {
        private readonly EStrategy strategy;
        private IRankingStrategy rankingStrategy;

        private readonly int[,] testcaseMatrix;
        private readonly string[] functionNames;


        public FaultLocalizer(int[,] testcaseMatrix, string[] fctNames, EStrategy strategy)
        {
            this.testcaseMatrix = testcaseMatrix;
            this.functionNames = fctNames;
            this.strategy = strategy;
            createRankingStrategyInstance();
        }

        public List<Item> CalculateSuspiciousnessRanking()
        {
            List<Item> suspiciousnessList = new List<Item>();

            int dim1 = testcaseMatrix.GetLength(0);
            int dim2 = testcaseMatrix.GetLength(1);
            int indexResult = testcaseMatrix.GetUpperBound(1);

            for (int j = 0; j < dim2 - 1; j++)
            {
                string functionName = functionNames[j];

                if (j < testcaseMatrix.GetUpperBound(1))
                {
                    int coveredFailed = 0;
                    int uncoveredFailed = 0;
                    int coveredPassed = 0;
                    int uncoveredPassed = 0;

                    for (int i = 0; i < dim1; i++)
                    {
                        if (testcaseMatrix[i, indexResult] == 0)
                        {
                            if (testcaseMatrix[i, j] == 0) uncoveredFailed++;
                            else coveredFailed++;
                        }
                        else
                        {
                            if (testcaseMatrix[i, j] == 0) uncoveredPassed++;
                            else coveredPassed++;
                        }
                    }

                    double suspiciousnessValue = rankingStrategy.calculateSuspiciousness(coveredFailed, uncoveredFailed, coveredPassed, uncoveredPassed);
                    suspiciousnessList.Add(new Item(functionName, suspiciousnessValue));
                }
            }

            suspiciousnessList.Sort();
            suspiciousnessList.Reverse();

            return suspiciousnessList;
        }

        private void createRankingStrategyInstance()
        {
            switch (strategy)
            {
                case EStrategy.Dstar:
                    rankingStrategy = new Dstar();
                    break;
                case EStrategy.Dstar2:
                    rankingStrategy = new Dstar(2);
                    break;
                case EStrategy.Dstar3:
                    rankingStrategy = new Dstar(3);
                    break;
                case EStrategy.Dstar4:
                    rankingStrategy = new Dstar(4);
                    break;
                case EStrategy.Dstar5:
                    rankingStrategy = new Dstar(5);
                    break;
                case EStrategy.Jaccard:
                    rankingStrategy = new Jaccard();
                    break;
                case EStrategy.Ochiai:
                    rankingStrategy = new Ochiai();
                    break;
                case EStrategy.Tarantula:
                    rankingStrategy = new Tarantula();
                    break;
                case EStrategy.RojersTanimoto:
                    rankingStrategy = new RojersTanimoto();
                    break;
                case EStrategy.SokalSneath:
                    rankingStrategy = new SokalSneath();
                    break;

                case EStrategy.DstarCS:
                    rankingStrategy = new DstarCS();
                    break;
                case EStrategy.DstarCS2:
                    rankingStrategy = new DstarCS(2);
                    break;
                case EStrategy.DstarCS3:
                    rankingStrategy = new DstarCS(3);
                    break;
                case EStrategy.DstarCS4:
                    rankingStrategy = new DstarCS(4);
                    break;
                case EStrategy.DstarCS5:
                    rankingStrategy = new DstarCS(5);
                    break;
                case EStrategy.JaccardCS:
                    rankingStrategy = new JaccardCS();
                    break;
                case EStrategy.OchiaiCS:
                    rankingStrategy = new OchiaiCS();
                    break;
                case EStrategy.TarantulaCS:
                    rankingStrategy = new TarantulaCS();
                    break;
                case EStrategy.RojersTanimotoCS:
                    rankingStrategy = new RojersTanimotoCS();
                    break;
                case EStrategy.SokalSneathCS:
                    rankingStrategy = new SokalSneathCS();
                    break;
            }
        }
    }
}
