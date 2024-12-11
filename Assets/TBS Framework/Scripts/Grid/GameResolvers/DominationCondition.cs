using System.Linq;

namespace TbsFramework.Grid.GameResolvers
{
    public class DominationCondition : GameEndCondition
    {
        public override GameResult CheckCondition(CellGrid cellGrid)
        {
            var playersAlive = cellGrid.Units.Select(u => u.PlayerNumber).Distinct().ToList();
            if (playersAlive.Count == 1)
            {
                var playersDead = cellGrid.Players.Where(p => p.PlayerNumber != playersAlive[0])
                                                  .Select(p => p.PlayerNumber)
                                                  .ToList();

                return new GameResult(true, playersAlive, playersDead);
            }
            return new GameResult(false, null, null);
        }
    }
}

