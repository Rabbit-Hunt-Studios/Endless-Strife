using System.Collections;
using TbsFramework.Grid;
using TbsFramework.Players.AI;
using TbsFramework.Units;
using UnityEngine;

namespace TbsFramework.Players.AI.Actions
{
    public abstract class MinimaxAIAction : MonoBehaviour
    {
        public abstract void InitializeAction(Player player, Unit unit, CellGrid cellGrid);
        public abstract bool ShouldExecute(Player player, Unit unit, CellGrid cellGrid);
        public abstract void Precalculate(Player player, Unit unit, CellGrid cellGrid);
        public abstract IEnumerator Execute(Player player, Unit unit, CellGrid cellGrid);
        public abstract void CleanUp(Player player, Unit unit, CellGrid cellGrid);
        public abstract void ShowDebugInfo(Player player, Unit unit, CellGrid cellGrid);
        
        // Minimax simulation - simulates this action's effect on the game state
        public virtual void SimulateAction(GameState state, Unit unit)
        {
            // Default implementation does nothing
            // Override in specific actions to simulate their effects
        }
        
        // Used by minimax to identify action types
        public virtual int GetActionIndex()
        {
            return 0; // Default - override in specific action types
        }
    }
}