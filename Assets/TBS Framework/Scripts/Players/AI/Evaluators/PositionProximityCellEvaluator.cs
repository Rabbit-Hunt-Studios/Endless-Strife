using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Evaluators
{
    public class PositionProximityCellEvaluator : CellEvaluator
    {
        public Cell Position;
        public float score;
        [Range(0, 1)]
        public float decayFactor = 0.9f;
        public int cutoff = 4;

        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            var distance = cellToEvaluate.GetDistance(Position);
            return distance <= cutoff ? score * Mathf.Pow(1 - decayFactor, distance) : 0;
        }
    }
}
