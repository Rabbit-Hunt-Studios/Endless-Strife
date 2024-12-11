using System.Linq;
using TbsFramework.Cells;
using TbsFramework.Grid;
using TbsFramework.Grid.GridStates;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;

namespace TbsFramework.Example4
{
    public class DeselectUnitAbility : Ability
    {
        public override void OnUnitClicked(Unit unit, CellGrid cellGrid)
        {
            if (cellGrid.GetCurrentPlayerUnits().Contains(unit))
            {
                cellGrid.cellGridState = new CellGridStateAbilitySelected(cellGrid, unit, unit.GetComponents<Ability>().ToList());
            }
        }

        public override void OnCellClicked(Cell cell, CellGrid cellGrid)
        {
            cellGrid.cellGridState = new CellGridStateWaitingForInput(cellGrid);
        }

        public override bool CanPerform(CellGrid cellGrid)
        {
            return true;
        }
    }
}
