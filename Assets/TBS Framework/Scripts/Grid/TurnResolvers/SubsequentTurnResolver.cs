using System.Linq;

namespace TbsFramework.Grid.TurnResolvers
{
    public class SubsequentTurnResolver : TurnResolver
    {
        public override TransitionResult ResolveStart(CellGrid cellGrid)
        {
            var nextPlayerNumber = cellGrid.Players.Min(p => p.PlayerNumber);
            var nextPlayer = cellGrid.Players.Find(p => p.PlayerNumber == nextPlayerNumber);
            var allowedUnits = cellGrid.Units.FindAll(u => u.PlayerNumber == nextPlayerNumber);

            return new TransitionResult(nextPlayer, allowedUnits);
        }

        public override TransitionResult ResolveTurn(CellGrid cellGrid)
        {
            var nextPlayerNumber = (cellGrid.CurrentPlayerNumber + 1) % cellGrid.NumberOfPlayers;
            while (cellGrid.Units.FindAll(u => u.PlayerNumber.Equals(nextPlayerNumber)).Count == 0)
            {
                nextPlayerNumber = (nextPlayerNumber + 1) % cellGrid.NumberOfPlayers;
            }

            var nextPlayer = cellGrid.Players.Find(p => p.PlayerNumber == nextPlayerNumber);
            var allowedUnits = cellGrid.Units.FindAll(u => u.PlayerNumber == nextPlayerNumber);

            return new TransitionResult(nextPlayer, allowedUnits);
        }
    }
}