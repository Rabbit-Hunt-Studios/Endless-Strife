using System.Linq;
using TbsFramework.Units;
using TbsFramework.Units.Abilities;

namespace TbsFramework.Grid.GridStates
{
    public class CellGridStateWaitingForInput : CellGrid.CellGridState
    {
        public CellGridStateWaitingForInput(CellGrid cellGrid) : base(cellGrid)
        {
        }

        public override void OnUnitClicked(Unit unit)
        {
            if (_cellGrid.GetCurrentPlayerUnits().Contains(unit))
            {
                _cellGrid.cellGridState = new CellGridStateAbilitySelected(_cellGrid, unit, unit.GetComponents<Ability>().ToList());
            }
        }
    }
}
