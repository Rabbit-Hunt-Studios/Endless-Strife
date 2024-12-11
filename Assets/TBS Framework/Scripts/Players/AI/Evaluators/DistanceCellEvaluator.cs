using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Evaluators
{
    public class DistanceCellEvaluator : CellEvaluator
    {
        public int maxTurnsToGetThere = 3;

        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            if (cellToEvaluate.Equals(evaluatingUnit.Cell))
            {
                return 0;
            }
            var path = evaluatingUnit.FindPath(cellGrid.Cells, cellToEvaluate);
            var pathCost = path.Sum(c => c.MovementCost);
            if (pathCost == 0)
            {
                return -1;
            }

            var turnsToGetThere = Mathf.Ceil(pathCost / evaluatingUnit.MovementPoints);
            return turnsToGetThere > maxTurnsToGetThere ? -1 : (turnsToGetThere - 1) / maxTurnsToGetThere * (-1);
        }
    }
}
