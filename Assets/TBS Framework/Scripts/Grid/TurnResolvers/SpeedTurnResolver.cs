using System.Collections.Generic;
using System.Linq;
using TbsFramework.Units;

namespace TbsFramework.Grid.TurnResolvers
{
    public class SpeedTurnResolver : TurnResolver
    {
        private Queue<Unit> unitsOrdered;

        public override TransitionResult ResolveStart(CellGrid cellGrid)
        {
            cellGrid.Units.ForEach(u => u.UnitDestroyed += OnUnitDestroyed);

            unitsOrdered = new Queue<Unit>(cellGrid.Units.OrderByDescending(u => u.GetComponent<Speed>().Value));
            var nextUnit = unitsOrdered.Dequeue();
            var nextPlayer = cellGrid.Players.Find(p => p.PlayerNumber == nextUnit.PlayerNumber);

            unitsOrdered.Enqueue(nextUnit);

            TransitionResult transitionResult = new TransitionResult(nextPlayer, new List<Unit>() { nextUnit });
            return transitionResult;
        }

        public override TransitionResult ResolveTurn(CellGrid cellGrid)
        {
            var nextUnit = unitsOrdered.Dequeue();
            var nextPlayer = cellGrid.Players.Find(p => p.PlayerNumber == nextUnit.PlayerNumber);

            unitsOrdered.Enqueue(nextUnit);

            TransitionResult transitionResult = new TransitionResult(nextPlayer, new List<Unit>() { nextUnit });
            return transitionResult;
        }
        private void OnUnitDestroyed(object sender, AttackEventArgs e)
        {
            unitsOrdered = new Queue<Unit>(unitsOrdered.Where(u => !u.Equals(e.Defender)));
        }
    }
}
