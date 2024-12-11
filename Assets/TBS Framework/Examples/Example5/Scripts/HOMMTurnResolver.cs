using System.Linq;
using TbsFramework.Grid;
using TbsFramework.Grid.TurnResolvers;

namespace TbsFramework.HOMMExample
{
    public class HOMMTurnResolver : SpeedTurnResolver
    {
        public override TransitionResult ResolveStart(CellGrid cellGrid)
        {
            var transitionResult = base.ResolveStart(cellGrid);

            var heroUnit = cellGrid.Units.Where(u => u.PlayerNumber == transitionResult.NextPlayer.PlayerNumber && (u as HOMMUnit).IsHero).First();
            var playableUnits = transitionResult.PlayableUnits();
            playableUnits.Add(heroUnit);

            return new TransitionResult(transitionResult.NextPlayer, playableUnits);
        }

        public override TransitionResult ResolveTurn(CellGrid cellGrid)
        {
            var transitionResult = base.ResolveTurn(cellGrid);
            if ((transitionResult.PlayableUnits()[0] as HOMMUnit).IsHero)
            {
                return ResolveTurn(cellGrid);
            }

            var heroUnit = cellGrid.Units.Where(u => u.PlayerNumber == transitionResult.NextPlayer.PlayerNumber && (u as HOMMUnit).IsHero).First();
            var playableUnits = transitionResult.PlayableUnits();
            playableUnits.Add(heroUnit);

            return new TransitionResult(transitionResult.NextPlayer, playableUnits);
        }
    }
}