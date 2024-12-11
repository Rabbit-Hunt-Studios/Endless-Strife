using System.Collections;
using TbsFramework.Grid;
using TbsFramework.Units;
using UnityEngine;


namespace TbsFramework.Players.AI.Actions
{
    public abstract class AIAction : MonoBehaviour
    {
        public abstract void InitializeAction(Player player, Unit unit, CellGrid cellGrid);
        public abstract bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid);
        public abstract void Precalculate(Player player, Unit unit, CellGrid cellGrid);
        public abstract IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid);
        public abstract void CleanUp(Player player, Unit unit, CellGrid cellGrid);
        public abstract void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid);
    }
}

