using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Evaluators
{
    public class UnitProximityCellEvaluator : CellEvaluator
    {
        public Unit TargetUnit;
        [Range(0, 1)]
        public float decayFactor = 0.9f;
        public int cutoff = 4;

        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            if (TargetUnit == null || cellToEvaluate.Equals(TargetUnit.Cell))
            {
                return 0;
            }

            var distance = cellToEvaluate.GetDistance(TargetUnit.Cell);
            return distance <= cutoff ? 1 * Mathf.Pow(1 - decayFactor, distance - 1) : 0;
        }
    }
}
