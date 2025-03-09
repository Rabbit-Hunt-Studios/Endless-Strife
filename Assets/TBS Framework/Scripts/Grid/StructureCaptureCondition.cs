using System.Collections.Generic;
using TbsFramework.Grid;
using TbsFramework.Grid.GameResolvers;
using TbsFramework.Units;

namespace TbsFramework.Grid
{
    public class StructureCaptureCondition : GameEndCondition
    {
        public Unit StructureToCapture;
        public int TargetPlayerNumber;
        public override GameResult CheckCondition(CellGrid cellGrid)
        {
            if (StructureToCapture.PlayerNumber == TargetPlayerNumber)
            {
                SaveAbility.PlayerData toAdd = new SaveAbility.PlayerData();
                toAdd.totalWins = 1;
                cellGrid.CurrentPlayer.GetComponent<SaveAbility>().UpdateValues(toAdd);
                return new GameResult(true, new List<int> { TargetPlayerNumber }, new List<int>());
            }

            return new GameResult(false, new List<int>(), new List<int>());
        }
    }
}
