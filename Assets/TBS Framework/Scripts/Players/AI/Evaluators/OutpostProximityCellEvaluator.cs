using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Evaluators
{
    public class OutpostProximityCellEvaluator : CellEvaluator
    {
        public int maxTurnsToGetThere = 3;
        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            if (cellToEvaluate.CurrentUnits.Count > 0)
            {
                foreach (var unit in cellToEvaluate.CurrentUnits)
                {
                    if (unit == null || !unit.isActiveAndEnabled)
                        continue;
                    ESUnit esUnit = unit.GetComponent<ESUnit>();
                    if (esUnit != null && 
                        esUnit.isStructure &&
                        esUnit.UnitName.Contains("Outpost") &&
                        unit.PlayerNumber != currentPlayer.PlayerNumber)
                    {
                        var path = evaluatingUnit.FindPath(cellGrid.Cells, cellToEvaluate);
                        var pathCost = path.Sum(c => c.MovementCost);
                        if (pathCost == 0)
                        {
                            return -1;
                        }
                        var distance = Mathf.Ceil(pathCost / evaluatingUnit.MovementPoints);
                        return distance > maxTurnsToGetThere ? -1 : distance / maxTurnsToGetThere;
                    }
                }
            }
            return -1;
        }
    }
}