using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Example4
{
    public class CapturingUnitEvaluator : UnitEvaluator
    {
        public override float Evaluate(Unit unitToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            var capturable = unitToEvaluate.Cell.CurrentUnits.Where(u => u.GetComponent<CapturableAbility>() != null).FirstOrDefault();
            var capturing = unitToEvaluate.GetComponent<CaptureAbility>()?.UnitReference;

            if (capturable == null || capturing == null || capturable.PlayerNumber != currentPlayer.PlayerNumber || capturing.PlayerNumber == currentPlayer.PlayerNumber)
            {
                return 0;
            }

            var captureAmount = (int)Mathf.Ceil(capturing.HitPoints * 10f / capturing.TotalHitPoints);
            return Mathf.Clamp(captureAmount / (float)capturable.GetComponent<CapturableAbility>().Loyality, 0, 1);
        }
    }
}

