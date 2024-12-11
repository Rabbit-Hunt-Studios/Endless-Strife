using System;
using TbsFramework.Grid.GameResolvers;

namespace TbsFramework.Grid
{
    public class GameEndedArgs : EventArgs
    {
        public GameResult gameResult { get; set; }

        public GameEndedArgs(GameResult result)
        {
            gameResult = result;
        }
    }
}

