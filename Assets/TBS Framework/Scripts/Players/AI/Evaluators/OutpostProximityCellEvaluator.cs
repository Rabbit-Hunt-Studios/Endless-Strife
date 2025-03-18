using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Evaluators
{
    public class OutpostProximityCellEvaluator : CellEvaluator
    {
        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            float bestScore = 0;
            
            foreach (var cell in cellGrid.Cells)
            {
                if (cell.CurrentUnits.Count > 0)
                {
                    foreach (var unit in cell.CurrentUnits)
                    {
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
                            float score = distance > 3 ? -1 : distance / 3;
                            
                            if (score > bestScore)
                                bestScore = score;
                        }
                    }
                }
            }
            
            return bestScore;
        }
    }
}