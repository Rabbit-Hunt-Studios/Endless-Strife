using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Units;

namespace TbsFramework.Example4
{
    public class CounterSpawnCondition : SpawnCondition
    {
        public override bool ShouldSpawn(CellGrid cellGrid, Unit unit, Player player)
        {
            var FriendlyHP = cellGrid.GetPlayerUnits(player)
                           .Where(u => u.GetComponent<AdvWrsUnit>().UnitName.Equals(unit.GetComponent<AdvWrsUnit>().UnitName))
                           .Sum(u => u.HitPoints);


            var MaxEnemyHP = 0f;
            foreach (var enemyPlayer in cellGrid.Players)
            {
                if (enemyPlayer.PlayerNumber.Equals(player.PlayerNumber))
                {
                    continue;
                }
                var EnemyHP = cellGrid.GetEnemyUnits(player)
                           .Where(u => u.GetComponent<AdvWrsUnit>().UnitName.Equals(unit.GetComponent<AdvWrsUnit>().UnitName))
                           .Sum(u => u.HitPoints);

                if (EnemyHP > MaxEnemyHP)
                {
                    MaxEnemyHP = EnemyHP;
                }
            }

            return MaxEnemyHP > FriendlyHP * 1.5f;
        }
    }
}

