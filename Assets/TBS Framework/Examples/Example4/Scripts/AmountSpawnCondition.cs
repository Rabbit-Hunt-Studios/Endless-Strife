using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Units;

namespace TbsFramework.Example4
{
    public class AmountSpawnCondition : SpawnCondition
    {
        public int TargetAmount;
        public override bool ShouldSpawn(CellGrid cellGrid, Unit unit, Player player)
        {
            var amount = cellGrid.GetPlayerUnits(player)
                           .Where(u => u.GetComponent<AdvWrsUnit>().UnitName.Equals(unit.GetComponent<AdvWrsUnit>().UnitName))
                           .Count();

            return amount < TargetAmount;
        }
    }
}
