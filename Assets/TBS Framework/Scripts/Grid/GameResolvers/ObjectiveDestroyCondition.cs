using System.Collections.Generic;
using System.Linq;
using TbsFramework.Units;

namespace TbsFramework.Grid.GameResolvers
{
    public class ObjectiveDestroyCondition : GameEndCondition
    {
        public Unit Objective;

        public override GameResult CheckCondition(CellGrid cellGrid)
        {
            var ObjectiveDestroyed = !cellGrid.Units.Exists(u => u.Equals(Objective));


            if (ObjectiveDestroyed)
            {
                List<int> winningPlayers = cellGrid.Players.Where(p => p.PlayerNumber != Objective.PlayerNumber)
                                                           .Select(p => p.PlayerNumber)
                                                           .ToList();
                List<int> losingPlayers = new List<int> { Objective.PlayerNumber };
                return new GameResult(true, winningPlayers, losingPlayers);
            }
            else
            {
                return new GameResult(false, new List<int>(), new List<int>());
            }
        }
    }
}