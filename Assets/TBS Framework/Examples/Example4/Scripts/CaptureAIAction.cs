using System.Collections;
using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Players.AI.Actions;
using TbsFramework.Units;

namespace TbsFramework.Example4
{
    public class CaptureAIAction : AIAction
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

            var capturable = unit.Cell.CurrentUnits.Select(u => u.GetComponent<CapturableAbility>())
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
    }
}
