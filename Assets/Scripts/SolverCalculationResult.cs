using System.Collections.Generic;

namespace SimonShapesModule
{
    public class SolverCalculationResult
    {
        public List<Stage> Stages { get; private set; }
        public List<List<int>> FinalShapes { get; private set; }

        public SolverCalculationResult(List<Stage> stages, List<List<int>> finalShapes)
        {
            Stages = stages;
            FinalShapes = finalShapes;
        }
    }
}
