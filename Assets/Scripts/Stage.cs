using System.Collections.Generic;

namespace SimonShapesModule
{
    public class Stage 
    {
        public int ReferenceDigit { get; private set; }
        public List<int> Flashes { get; private set; }
        public Pair<SimonShapesColor, SimonShapesColor> StageAnswer { get; private set; }
        public bool Submitted { get; set; }

        public Stage(int referenceDigit, List<int> flashes, Pair<SimonShapesColor, SimonShapesColor> stageAnswer)
        {
            ReferenceDigit = referenceDigit;
            Flashes = flashes;
            StageAnswer = stageAnswer;
            Submitted = false;
        }
    }    
}
