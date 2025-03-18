using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Evaluators
{
    public class ObjectiveProximityCellEvaluator : CellEvaluator
    {
        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            Cell objectiveCell = null;
            foreach (var cell in cellGrid.Cells)
            {
                if (cell.CurrentUnits.Count > 0)
                {
                    foreach (var unit in cell.CurrentUnits)
                    {
                        ESUnit esUnit = unit.GetComponent<ESUnit>();
                        if (esUnit != null && 
                            esUnit.isStructure &&
                            esUnit.UnitName.Contains("Objective") &&
                            unit.PlayerNumber != currentPlayer.PlayerNumber)
                        {
                            objectiveCell = cell;
                            break;
                        }
                    }
                }
                
                if (objectiveCell != null)
                    break;
            }
            
            if (objectiveCell == null)
                return -1;
                
            var path = evaluatingUnit.FindPath(cellGrid.Cells, cellToEvaluate);
            var pathCost = path.Sum(c => c.MovementCost);
            if (pathCost == 0)
            {
                return -1;
            }
            var distance = Mathf.Ceil(pathCost / evaluatingUnit.MovementPoints);

            return distance > 3 ? -1 : distance / 3;
        }
    }
}