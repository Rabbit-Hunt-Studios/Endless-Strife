using System.Collections;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Players.AI.Actions;
using TbsFramework.Units;

public class  MinimaxCaptureAction : MinimaxAIAction
{
    public override void InitializeAction(Player player, Unit unit, CellGrid cellGrid)
    {
        unit.GetComponent<CaptureAbility>().OnAbilitySelected(cellGrid);
    }
    public override bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid)
    {
        if (unit.GetComponent<CaptureAbility>() == null)
        {
            return false;
        }

        var capturable = unit.Cell.CurrentUnits.Where(u => u != null && u.gameObject != null)
                                                                .Select(u => u.GetComponent<CapturableAbility>())
                                                                .OfType<CapturableAbility>()
                                                                .ToList();

        return unit.GetComponent<CaptureAbility>() != null && capturable.Count > 0 && capturable[0].GetComponent<Unit>().PlayerNumber != unit.PlayerNumber && unit.ActionPoints > 0;
    }
    public override void CleanUp(Player player, Unit unit, CellGrid cellGrid)
    {
    }
    public override IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid)
    {
        yield return StartCoroutine(unit.GetComponent<CaptureAbility>().AIExecute(cellGrid));
        yield return null;
    }
    public override void Precalculate(Player player, Unit unit, CellGrid cellGrid)
    {
    }
    public override void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid)
    {
    }

    public override int GetActionIndex()
    {
        return 3;
    }
}

