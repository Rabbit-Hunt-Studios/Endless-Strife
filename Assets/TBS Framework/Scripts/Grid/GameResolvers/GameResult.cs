using System.Collections.Generic;

namespace TbsFramework.Grid.GameResolvers
{
    public class GameResult
    {
        public GameResult(bool isFinished, List<int> winningPlayers, List<int> loosingPlayers)
        {
            IsFinished = isFinished;
            WinningPlayers = winningPlayers;
            LoosingPlayers = loosingPlayers;
        }

        public bool IsFinished { get; private set; }

        public List<int> WinningPlayers { get; private set; }
        public List<int> LoosingPlayers { get; private set; }
    }
}

