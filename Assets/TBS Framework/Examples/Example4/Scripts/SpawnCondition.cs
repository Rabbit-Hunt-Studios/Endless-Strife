using TbsFramework.Grid;
using TbsFramework.Players;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Example4
{
    public abstract class SpawnCondition : MonoBehaviour
    {
        public abstract bool ShouldSpawn(CellGrid cellGrid, Unit unit, Player player);
    }
}
