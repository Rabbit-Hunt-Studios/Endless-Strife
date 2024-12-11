using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TbsFramework.Grid.GameResolvers
{
    public class TurnLimitCondition : GameEndCondition
    {
        public int TurnLimit;
        [Tooltip("Specifies a single player that the condition applies to")]
        public int AppliesToPlayerNo;
        [Tooltip("Specifies whether the player wil win (positive) or lose (negative) when time runs out")]
        public bool isPositive;

        private int nEndTurns;
        private int nTurn;

        private void Start()
        {
            GetComponent<CellGrid>().TurnEnded += OnTurnEnded;
        }

        private void OnTurnEnded(object sender, bool isNetworkInvoked)
        {
            nEndTurns++;
            var distinctPlayersAlive = (sender as CellGrid).Units.Select(u => u.PlayerNumber)
                                                                 .Distinct()
                                                                 .ToList().Count;
            if (nEndTurns % distinctPlayersAlive == 0)
            {
                nTurn += 1;
            }
        }

        public override GameResult CheckCondition(CellGrid cellGrid)
        {
            var isFinished = cellGrid.CurrentPlayerNumber == AppliesToPlayerNo && nTurn >= TurnLimit;
            if (isFinished)
            {
                var firstGroup = new List<int>() { AppliesToPlayerNo };
                var secondGroup = cellGrid.Players.Where(p => p.PlayerNumber != AppliesToPlayerNo)
                                                     .Select(p => p.PlayerNumber)
                                                     .ToList();

                var winningPlayers = isPositive ? firstGroup : secondGroup;
                var loosingPlayers = isPositive ? secondGroup : firstGroup;

                return new GameResult(true, winningPlayers, loosingPlayers);
            }

            return new GameResult(false, null, null);
        }
    }
}

