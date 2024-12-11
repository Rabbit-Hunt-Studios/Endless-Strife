using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units;

namespace TbsFramework.Example4
{
    public class CaptureCellEvaluator : CellEvaluator
    {
        public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
        {
            var capturable = cellToEvaluate.CurrentUnits.Select(u => u.GetComponent<CapturableAbility>())
                                                                  .OfType<CapturableAbility>()
                                                                  .ToList();

            var capturing = cellToEvaluate.CurrentUnits.Select(u => u.GetComponent<CaptureAbility>())
                                                                  .OfType<CaptureAbility>()
                                                                  .ToList();
            var isCapturable = false;
            var score = 0f;
            if (capturable.Count > 0)
            {
                isCapturable = capturable[0].GetComponent<Unit>().PlayerNumber != currentPlayer.PlayerNumber && (capturing.Count > 0 ? (capturing[0].GetComponent<Unit>().Equals(evaluatingUnit) || capturing[0].GetComponent<Unit>().PlayerNumber != currentPlayer.PlayerNumber) : true);
                score = ((float)(capturable[0].GetComponent<CapturableAbility>().MaxLoyality - (float)capturable[0].GetComponent<CapturableAbility>().Loyality) / (float)capturable[0].GetComponent<CapturableAbility>().MaxLoyality);
            }

            return isCapturable ? 1 + score : 0;
        }
    }
}

