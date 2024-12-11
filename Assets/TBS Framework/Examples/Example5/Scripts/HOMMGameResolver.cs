using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Grid.GameResolvers;

namespace TbsFramework.HOMMExample
{
    public class HOMMGameResolver : GameEndCondition
    {
        public override GameResult CheckCondition(CellGrid cellGrid)
        {
            var playersAlive = cellGrid.Units.Where(u => !(u as HOMMUnit).IsHero).Select(u => u.PlayerNumber).Distinct().ToList();
            if (playersAlive.Count == 1)
            {
                var playersDead = cellGrid.Players.FindAll(p => p.PlayerNumber != playersAlive[0])
                                                  .Select(p => p.PlayerNumber)
                                                  .ToList();

                return new GameResult(true, playersAlive, playersDead);
            }
            return new GameResult(false, null, null);
        }
    }
}