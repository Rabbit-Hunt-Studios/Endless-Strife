using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Evaluators
{
    public class SafetyCellEvaluator : CellEvaluator
    {
        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            float dangerLevel = 0;
            
            foreach (var unit in cellGrid.Units)
            {
                if (unit.PlayerNumber != currentPlayer.PlayerNumber)
                {
                    if (unit.IsUnitAttackable(evaluatingUnit, cellToEvaluate))
                    {
                        dangerLevel += unit.AttackFactor;
                    }
                    
                    float distance = Vector3.Distance(unit.Cell.transform.position, cellToEvaluate.transform.position);
                    if (distance <= unit.MovementPoints + unit.AttackRange && unit.ActionPoints >= 1)
                    {
                        dangerLevel += unit.AttackFactor * 0.5f; // Less dangerous than immediate threats
                    }
                }
            }
            
            return 10 - Mathf.Min(10, dangerLevel);
        }
    }
}