using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Example4
{
    public class CaptureProtectionEvaluator : CellEvaluator
    {
        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            var neighbours = cellToEvaluate.GetNeighbours(cellGrid.Cells);
            foreach (var neighbour in neighbours)
            {
                if (neighbour.CurrentUnits.Count == 0)
                {
                    continue;
                }

                var capturable = neighbour.CurrentUnits.Where(u => u.GetComponent<CapturableAbility>() != null).FirstOrDefault();
                var capturing = neighbour.CurrentUnits.Where(u => u.GetComponent<CapturableAbility>() == null).FirstOrDefault();

                if (capturable == null || capturing == null || capturable.PlayerNumber != currentPlayer.PlayerNumber || capturing.PlayerNumber == currentPlayer.PlayerNumber)
                {
                    continue;
                }

                var captureAmount = (int)Mathf.Ceil(capturing.HitPoints * 10f / capturing.TotalHitPoints);

                return Mathf.Clamp(captureAmount / (float)capturable.GetComponent<CapturableAbility>().Loyality, 0, 1);

            }
            return 0;
        }
    }
}

