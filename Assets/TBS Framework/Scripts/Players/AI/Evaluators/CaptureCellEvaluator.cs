using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Players.AI.Evaluators;
using TbsFramework.Units;

public class CaptureCellEvaluator : CellEvaluator
{
    public override float Evaluate(Cell cellToEvaluate, Unit evaluatingUnit, Player currentPlayer, CellGrid cellGrid)
    {
        var capturable = cellToEvaluate.CurrentUnits.Where(u => u != null && u.gameObject != null)
                                                                .Select(u => u.GetComponent<CapturableAbility>())
                                                                .OfType<CapturableAbility>()
                                                                .ToList();

        var capturing = cellToEvaluate.CurrentUnits.Where(u => u != null && u.gameObject != null)
                                                                .Select(u => u.GetComponent<CaptureAbility>())
                                                                .OfType<CaptureAbility>()
                                                                .ToList();
        var isCapturable = false;
        if (capturable.Count > 0)
        {
            isCapturable = capturable[0].GetComponent<Unit>().PlayerNumber != currentPlayer.PlayerNumber && (capturing.Count > 0 ? (capturing[0].GetComponent<Unit>().Equals(evaluatingUnit) || capturing[0].GetComponent<Unit>().PlayerNumber != currentPlayer.PlayerNumber) : true);
        }

        return isCapturable ? 1 : 0;
    }
}


