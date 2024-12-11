using System;
using System.Collections.Generic;
using TbsFramework.Players;
using TbsFramework.Units;

namespace TbsFramework.Grid.TurnResolvers
{
    public class TransitionResult
    {
        public Player NextPlayer { get; private set; }
        public Func<List<Unit>> PlayableUnits { get; private set; }

        public TransitionResult(Player nextPlayer, Func<List<Unit>> allowedUnits)
        {
            NextPlayer = nextPlayer;
            PlayableUnits = allowedUnits;
        }

        public TransitionResult(Player nextPlayer, List<Unit> allowedUnits) : this(nextPlayer, () => allowedUnits)
        {
        }
    }
}
